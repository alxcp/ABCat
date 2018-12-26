using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.Catalog;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using HtmlAgilityPack;

namespace ABCat.Shared.Plugins.Sites
{
    public abstract class WebSiteParserBase : IWebSiteParserPlugin
    {
        public Config Config { get; set; }
        private int _webSiteId = -1;
        private HashSet<string> _groupKeys;

        public int WebSiteId
        {
            get
            {
                if (_webSiteId == -1)
                {
                    var dbContainer = Context.I.DbContainer;
                    var ws = dbContainer.WebSiteSet.GetWebSitesAll()
                        .FirstOrDefault(item => item.WebSiteParserPluginName.Compare(GetType().Name));
                    if (ws == null)
                    {
                        ws = dbContainer.WebSiteSet.CreateWebSite();
                        ws.WebSiteParserPluginName = GetType().Name;
                        dbContainer.WebSiteSet.AddWebSite(ws);
                        ws = dbContainer.WebSiteSet.GetWebSitesAll()
                            .First(item => item.WebSiteParserPluginName.Compare(GetType().Name));
                    }

                    _webSiteId = ws.Id;
                }

                return _webSiteId;
            }
        }

        public HashSet<string> GetGroupKeys(bool forceRefresh)
        {
            if (_groupKeys == null || forceRefresh)
            {
                var webSiteId = WebSiteId;
                var dbContainer = Context.I.DbContainer;
                _groupKeys = dbContainer.AudioBookGroupSet.GetRecordGroupsAll()
                    .Where(item => item.WebSiteId == webSiteId).Select(item => item.Key).ToHashSet();
            }

            return _groupKeys;
        }

        public abstract Uri GetRecordPageUrl(IAudioBook record);
        public abstract string DisplayName { get; }

        public async Task DownloadRecordGroups(HashSet<string> recordGroupsKeys, CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;

                    var groupActualityPeriod = mainConfig.GroupActualityPeriod;
                    var z = 0;
                    IAudioBookGroup[] groups;
                    if (recordGroupsKeys == null) groups = GetAllRecordGroups(dbContainer).ToArray();
                    else
                        groups =
                            GetAllRecordGroups(dbContainer)
                                .Where(item => recordGroupsKeys.Contains(item.Key))
                                .ToArray();

                    foreach (var group in groups.Where(item =>
                        (DateTime.Now - item.LastUpdate).TotalDays > groupActualityPeriod))
                    {
                        try
                        {
                            ProgressMessage.ReportComplex(z, groups.Length);
                            DownloadRecordGroup(dbContainer, group, cancellationToken);
                            if (cancellationToken.IsCancellationRequested) break;
                            dbContainer.SaveChanges();
                            z++;
                        }
                        catch (Exception ex)
                        {
                            // ignored
                        }
                    }
                }

            }, cancellationToken);
        }

        public async Task DownloadRecords(HashSet<string> recordsKeys, PageSources pageSource,
            CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                var recordActualityPeriod = mainConfig.RecordActualityPeriod;

                using(var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;

                    IAudioBook[] records;

                    if (recordsKeys == null)
                    {
                        if (pageSource == PageSources.CacheOnly)
                        {
                            records = dbContainer.AudioBookSet.GetRecordsByWebSite(WebSiteId).ToArray();
                        }
                        else
                        {
                            records =
                                dbContainer.AudioBookSet.GetRecordsUpdatedBefore(WebSiteId,
                                    DateTime.Now.Subtract(TimeSpan.FromDays(recordActualityPeriod))).ToArray();
                        }
                    }
                    else
                    {
                        records = dbContainer.AudioBookSet.GetRecordsByKeys(recordsKeys).ToArray();
                    }

                    var sw = new Stopwatch();
                    sw.Start();

                    var normalizerPlugin =
                        Context.I.ComponentFactory.GetCreators<INormalizationLogicPlugin>()
                            .First()
                            .GetInstance<INormalizationLogicPlugin>();

                    var waitingForSave = new List<IAudioBook>();

                    for (var z = 0; z < records.Length; z++)
                    {
                        try
                        {
                            var record = records[z];
                            ProgressMessage.ReportComplex(z, records.Length);
                            DownloadRecord(dbContainer, record, pageSource, cancellationToken);
                            record.LastUpdate = DateTime.Now;
                            waitingForSave.Add(record);

                            if (record.Created == default(DateTime))
                            {
                                record.Created = DateTime.Now;
                            }

                            if (cancellationToken.IsCancellationRequested) break;
                            if (sw.Elapsed > TimeSpan.FromSeconds(30) || z == records.Length - 1)
                            {
                                sw.Restart();
                                normalizerPlugin.Normalize(waitingForSave, dbContainer);

                                if (cancellationToken.IsCancellationRequested) break;
                                dbContainer.AudioBookSet.AddChangedRecords(waitingForSave.ToArray());
                                dbContainer.SaveChanges();
                                if (cancellationToken.IsCancellationRequested) break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Context.I.Logger.Error(ex);
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        dbContainer.SaveChanges();
                    }
                    else dbContainer.AutoSaveChanges = false;
                }
            }, cancellationToken);
        }

        public async Task OrganizeKeywords(CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;
                    var recordActualityPeriod = mainConfig.RecordActualityPeriod;
                    var records = dbContainer.AudioBookSet.GetRecordsAll().ToList();

                    var sw = new Stopwatch();
                    sw.Start();

                    var symbolicDistancePlugin = Context.I.ComponentFactory.GetActualCreator<ISymbolicDistance>();

                    var allGenres = records.SelectMany(item => item.Genre.Split(',')).Select(item => item.Trim())
                        .GroupBy(item => item)
                        .ToDictionary(item => item.Key, item => item.Count());

                    var topMost = allGenres.Where(item => item.Value > 3).OrderByDescending(item => item.Value)
                        .ToDictionary(item => item.Key, item => item.Value);

                    //var waitingForSave = new List<IAudioBook>();

                    //for (var z = 0; z < records.Count; z++)
                    //{
                    //    try
                    //    {
                    //        var record = records[z];
                    //        totalProgressCallback(z, records.Count, "{0} из {1}".F(z, records.Count()));
                    //        DownloadRecord(dbContainer, record, pageSource, smallProgressCallback,
                    //            cancellationToken);
                    //        record.LastUpdate = DateTime.Now;
                    //        waitingForSave.Add(record);

                    //        if (record.Created == default(DateTime))
                    //        {
                    //            record.Created = DateTime.Now;
                    //        }

                    //        if (cancellationToken.IsCancellationRequested) break;
                    //        if (sw.Elapsed > TimeSpan.FromSeconds(30) || z == records.Count - 1)
                    //        {
                    //            sw.Restart();
                    //            normalizerPlugin.Normalize(waitingForSave, dbContainer);

                    //            if (cancellationToken.IsCancellationRequested) break;
                    //            dbContainer.AudioBookSet.AddChangedRecords(waitingForSave.ToArray());
                    //            dbContainer.SaveChanges();
                    //            if (cancellationToken.IsCancellationRequested) break;
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Context.I.Logger.Error(ex);
                    //    }
                    //}

                    //if (!cancellationToken.IsCancellationRequested)
                    //{
                    //    dbContainer.SaveChanges();
                    //}
                    //else dbContainer.AutoSaveChanges = false;
                }
            }, cancellationToken);
        }

        protected virtual long GetSizeInBytes(string sizeString)
        {
            long result = 0;
            var subSize = sizeString.Split(' ');
            subSize[0] = subSize[0].Replace(',', '.');

            if (subSize.Length >= 2 && float.TryParse(subSize[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out var size))
            {
                int multiplier = 0;

                switch (subSize[1])
                {
                    case "Гб":
                    case "GB":
                        multiplier = 1024 * 1024 * 1024;
                        break;
                    case "Мб":
                    case "MB":
                        multiplier = 1024 * 1024;
                        break;
                    case "Кб":
                    case "KB":
                        multiplier = 1024;
                        break;
                    default:
                        var t = 0;
                        break;
                }

                result = (long)(size * multiplier);
            }

            return result;
        }

        public async Task<string> DownloadRecordSourcePage(IAudioBook audioBook, CancellationToken cancellationToken)
        {
            return await Task.Factory.StartNew(() => GetRecordSourcePageString(audioBook, PageSources.CacheOrWeb, cancellationToken), cancellationToken);
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfigs);
        protected abstract string[] RecordPageJunkIdList { get; }

        public void Dispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Disposed;

        public abstract IEnumerable<IAudioBookGroup> GetAllRecordGroups(IDbContainer container);
        private static readonly Regex HtmlSpecCharRegex = new Regex("&#[0-9]+;");
        private static readonly Regex HtmlEscCharRegex = new Regex("&[A-Za-z]+;");

        protected virtual string CleanupRecordValue(string value, bool allowMultiLine, int maxLength)
        {
            var result = allowMultiLine ? value : value.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)[0];

            do
            {
                value = result;
                result = value.Trim().Trim('\r', '\n', ':', '-', '=');
            } while (result != value);

            if (maxLength > 0 && result.Length > maxLength)
            {
                result = result.Substring(0, maxLength - 1) + "…";
            }

            result = HtmlSpecCharRegex.Replace(result, HtmlSpecCharRegexReplacer);
            result = HtmlEscCharRegex.Replace(result, HtmlSpecCharRegexReplacer);

            return result;
        }

        static string HtmlSpecCharRegexReplacer(Match m)
        {
            return WebUtility.HtmlDecode(m.Value);
        }

        /// <summary>
        ///     Очистить страницу описания торрента от лишних элементов (заголовок, меню, скрипты и др.)
        /// </summary>
        /// <param name="document">Документ</param>
        protected virtual void CleanupRecordPage(HtmlDocument document)
        {
            foreach (var script in document.GetNodes("script", "type", str => true).ToArray())
            {
                script.ParentNode.RemoveChild(script);
            }

            document.RemoveNodesByIds(RecordPageJunkIdList);
        }

        protected abstract void DownloadRecord(IDbContainer db, IAudioBook record, PageSources pageSource, CancellationToken cancellationToken);

        protected abstract void DownloadRecordGroup(IDbContainer db, IAudioBookGroup recordGroup, CancellationToken cancellationToken);

        protected abstract string GetRecordSourcePageString(IAudioBook audioBook, PageSources pageSource,
            CancellationToken cancellationToken);

        protected abstract void ParseRecord(IDbContainer db, IAudioBook record, string postBodyHtml);


        //private static class KeysCollection
        //{
        //    private static Stopwatch _sw = new Stopwatch();
        //    private static readonly Dictionary<string, HashSet<string>> _values = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);

        //    public static void Add(string key, string value)
        //    {
        //        if (!_values.TryGetValue(key, out var values))
        //        {
        //            values = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        //            _values.Add(key, values);
        //        }

        //        values.Add(value);

        //        if (!_sw.IsRunning)
        //            _sw.Start();

        //        if (_sw.Elapsed.TotalSeconds > 15)
        //        {
        //            Debug.WriteLine("===============================");
        //            var top50 = _values.Where(item => item.Value.Count > 10);
        //            foreach (var keyValuePair in top50.OrderByDescending(item=>item.Value.Count))
        //            {
        //                Debug.WriteLine($"{keyValuePair.Key}\t{keyValuePair.Value.Count}");
        //            }

        //            Debug.WriteLine("===============================");
        //            _sw.Restart();
        //        }
        //    }
        //}

        protected virtual void FillRecordElement(IAudioBook record, string key, string value)
        {
            key = key.Trim(' ', '[', ']', '<', '>', ';', '\n', '\t', '.', ',', '"', '(');

            //#if DEBUG
            //            KeysCollection.Add(key, value);
            //#endif

            switch (key.ToLower())
            {
                case "автор":
                case "авторы":
                case "фамилия, имя автора":
                case "фамилии авторов":
                case "авторы произведений":
                    record.Author = CleanupRecordValue(value, false, 500)
                        .ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "фамилия автора":
                    record.AuthorSurnameForParse =
                        CleanupRecordValue(value, false, 250).ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "имя автора":
                    record.AuthorNameForParse =
                        CleanupRecordValue(value, false, 250).ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "издательство":
                    record.Publisher =
                        CleanupRecordValue(value, false, 500).ChangeCase(Extensions.CaseTypes.FirstWord, true, true);
                    break;
                case "исполнитель":
                case "исполнители":
                case "запись и обработка":
                    record.Reader = CleanupRecordValue(value, false, 500)
                        .ChangeCase(Extensions.CaseTypes.AllWords, true, true);
                    break;
                case "жанр":
                    record.Genre =
                        CleanupRecordValue(value, false, 200)
                            .ChangeCase(Extensions.CaseTypes.AfterSplitter, false, true);
                    break;
                case "битрейт":
                case "битрейт аудио":
                    record.Bitrate = CleanupRecordValue(value, false, 100)
                        .ChangeCase(Extensions.CaseTypes.FirstWord, true, true);
                    break;
                case "длительность":
                case "прдолжительность":
                case "продолжительность":
                case "продолжительность звучания":
                case "продолжительность книги":
                case "общее время звучания":
                case "общее время воспроизведения":
                case "bремя звучания":
                case "время звучания":
                case "время воспроизведения":
                case "время чтения":
                case "общая продолжительность":
                case "продолжительность аудиокниги":
                case "продолжительность (время звучания)":
                case "время":
                case "длина записи":
                    record.Length = CleanupRecordValue(value, false, 500);
                    break;
                case "описание":
                case "аннотация":
                    record.Description = CleanupRecordValue(value, true, 1000);
                    break;
                case "доп. информация":
                    if (ParseKeyValue(value, out var secondKey, out var secondValue))
                    {
                        FillRecordElement(record, secondKey, secondValue);
                    }

                    break;
            }
        }

        protected bool ParseKeyValue(string keyValue, out string key, out string value)
        {
            var result = false;

            key = null;
            value = null;

            if (!keyValue.IsNullOrEmpty())
            {
                var iofColon = keyValue.IndexOf(':');
                if (iofColon > 0 && iofColon < keyValue.Length - 1)
                {
                    key = keyValue.Substring(0, iofColon).ToLower();
                    var iofSemiColon = key.LastIndexOf(';');
                    if (iofSemiColon > 0)
                    {
                        key = key.Substring(iofSemiColon + 1, key.Length - iofSemiColon - 1);
                    }

                    key = key.TrimStart('-', '\n', '.').Trim(' ');

                    if (key != string.Empty)
                    {
                        value = keyValue.Substring(iofColon + 1, keyValue.Length - iofColon - 1);
                        result = true;
                    }
                }
            }

            return result;
        }
    }
}