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
    static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }

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
                            ProgressMessage.ReportComplex(z, groups.Length, $"{DisplayName}: {z} / {groups.Length}");
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
                            ProgressMessage.ReportComplex(z, records.Length, $"{DisplayName}: {z} / {records.Length}");
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
                            Context.I.MainLog.Error(ex);
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

        //private class Word
        //{
        //    public string Text { get; set; }
        //    public List<int> Codes { get; set; } = new List<int>();
        //}

        //private int LevenshteinDistance(Word source, Word target, bool fullWord, bool translation)
        //{
        //    if (String.IsNullOrEmpty(source.Text))
        //    {
        //        if (String.IsNullOrEmpty(target.Text))
        //            return 0;
        //        return target.Text.Length * 2;
        //    }
        //    if (String.IsNullOrEmpty(target.Text))
        //        return source.Text.Length * 2;
        //    int n = source.Text.Length;
        //    int m = target.Text.Length;
        //    //TODO Убрать в параметры (для оптимизации)
        //    int[,] distance = new int[3, m + 1];
        //    // Initialize the distance 'matrix'
        //    for (var j = 1; j <= m; j++)
        //        distance[0, j] = j * 2;
        //    var currentRow = 0;
        //    for (var i = 1; i <= n; ++i)
        //    {
        //        currentRow = i % 3;
        //        var previousRow = (i - 1) % 3;
        //        distance[currentRow, 0] = i * 2;
        //        for (var j = 1; j <= m; j++)
        //        {
        //            distance[currentRow, j] = Math.Min(Math.Min(
        //                    distance[previousRow, j] + ((!fullWord && i == n) ? 2 - 1 : 2),
        //                    distance[currentRow, j - 1] + ((!fullWord && i == n) ? 2 - 1 : 2)),
        //                distance[previousRow, j - 1] + CostDistanceSymbol(source, i - 1, target, j - 1, translation));

        //            if (i > 1 && j > 1 && source.Text[i - 1] == target.Text[j - 2]
        //                && source.Text[i - 2] == target.Text[j - 1])
        //            {
        //                distance[currentRow, j] = Math.Min(distance[currentRow, j], distance[(i - 2) % 3, j - 2] + 2);
        //            }
        //        }
        //    }
        //    return distance[currentRow, m];
        //}

        //private int CostDistanceSymbol(Word source, int sourcePosition, Word search, int searchPosition, bool translation)
        //{
        //    if (source.Text[sourcePosition] == search.Text[searchPosition])
        //        return 0;
        //    if (translation)
        //        return 2;
        //    if (source.Codes[sourcePosition] != 0 && source.Codes[sourcePosition] == search.Codes[searchPosition])
        //        return 0;
        //    int resultWeight;
        //    if (!DistanceCodeKey.TryGetValue(source.Codes[sourcePosition], out var nearKeys))
        //        resultWeight = 2;
        //    else
        //        resultWeight = nearKeys.Contains(search.Codes[searchPosition]) ? 1 : 2;
        //    if (PhoneticGroupsRus.TryGetValue(search.Text[searchPosition], out var phoneticGroups))
        //        resultWeight = Math.Min(resultWeight, phoneticGroups.Contains(source.Text[sourcePosition]) ? 1 : 2);
        //    if (PhoneticGroupsEng.TryGetValue(search.Text[searchPosition], out phoneticGroups))
        //        resultWeight = Math.Min(resultWeight, phoneticGroups.Contains(source.Text[sourcePosition]) ? 1 : 2);
        //    return resultWeight;
        //}

        //#region Блок Фонетических групп
        //static Dictionary<char, List<char>> PhoneticGroupsRus = new Dictionary<char, List<char>>();
        //static Dictionary<char, List<char>> PhoneticGroupsEng = new Dictionary<char, List<char>>();
        //#endregion

        //static WebSiteParserBase()
        //{
        //    SetPhoneticGroups(PhoneticGroupsRus, new List<string>() { "ыий", "эе", "ая", "оёе", "ую", "шщ", "оа" });
        //    SetPhoneticGroups(PhoneticGroupsEng, new List<string>() { "aeiouy", "bp", "ckq", "dt", "lr", "mn", "gj", "fpv", "sxz", "csz" });
        //}

        //private static void SetPhoneticGroups(Dictionary<char, List<char>> resultPhoneticGroups, List<string> phoneticGroups)
        //{
        //    foreach (string group in phoneticGroups)
        //    foreach (char symbol in group)
        //        if (!resultPhoneticGroups.ContainsKey(symbol))
        //            resultPhoneticGroups.Add(symbol, phoneticGroups.Where(pg => pg.Contains(symbol)).SelectMany(pg => pg).Distinct().Where(ch => ch != symbol).ToList());
        //}

        ///// <summary>
        ///// Близость кнопок клавиатуры
        ///// </summary>
        //private static Dictionary<int, List<int>> DistanceCodeKey = new Dictionary<int, List<int>>
        //{
        //    /* '`' */ { 192 , new List<int>(){ 49 }},
        //    /* '1' */ { 49 , new List<int>(){ 50, 87, 81 }},
        //    /* '2' */ { 50 , new List<int>(){ 49, 81, 87, 69, 51 }},
        //    /* '3' */ { 51 , new List<int>(){ 50, 87, 69, 82, 52 }},
        //    /* '4' */ { 52 , new List<int>(){ 51, 69, 82, 84, 53 }},
        //    /* '5' */ { 53 , new List<int>(){ 52, 82, 84, 89, 54 }},
        //    /* '6' */ { 54 , new List<int>(){ 53, 84, 89, 85, 55 }},
        //    /* '7' */ { 55 , new List<int>(){ 54, 89, 85, 73, 56 }},
        //    /* '8' */ { 56 , new List<int>(){ 55, 85, 73, 79, 57 }},
        //    /* '9' */ { 57 , new List<int>(){ 56, 73, 79, 80, 48 }},
        //    /* '0' */ { 48 , new List<int>(){ 57, 79, 80, 219, 189 }},
        //    /* '-' */ { 189 , new List<int>(){ 48, 80, 219, 221, 187 }},
        //    /* '+' */ { 187 , new List<int>(){ 189, 219, 221 }},
        //    /* 'q' */ { 81 , new List<int>(){ 49, 50, 87, 83, 65 }},
        //    /* 'w' */ { 87 , new List<int>(){ 49, 81, 65, 83, 68, 69, 51, 50 }},
        //    /* 'e' */ { 69 , new List<int>(){ 50, 87, 83, 68, 70, 82, 52, 51 }},
        //    /* 'r' */ { 82 , new List<int>(){ 51, 69, 68, 70, 71, 84, 53, 52 }},
        //    /* 't' */ { 84 , new List<int>(){ 52, 82, 70, 71, 72, 89, 54, 53 }},
        //    /* 'y' */ { 89 , new List<int>(){ 53, 84, 71, 72, 74, 85, 55, 54 }},
        //    /* 'u' */ { 85 , new List<int>(){ 54, 89, 72, 74, 75, 73, 56, 55 }},
        //    /* 'i' */ { 73 , new List<int>(){ 55, 85, 74, 75, 76, 79, 57, 56 }},
        //    /* 'o' */ { 79 , new List<int>(){ 56, 73, 75, 76, 186, 80, 48, 57 }},
        //    /* 'p' */ { 80 , new List<int>(){ 57, 79, 76, 186, 222, 219, 189, 48 }},
        //    /* '[' */ { 219 , new List<int>(){ 48, 186, 222, 221, 187, 189 }},
        //    /* ']' */ { 221 , new List<int>(){ 189, 219, 187 }},
        //    /* 'a' */ { 65 , new List<int>(){ 81, 87, 83, 88, 90 }},
        //    /* 's' */ { 83 , new List<int>(){ 81, 65, 90, 88, 67, 68, 69, 87, 81 }},
        //    /* 'd' */ { 68 , new List<int>(){ 87, 83, 88, 67, 86, 70, 82, 69 }},
        //    /* 'f' */ { 70 , new List<int>(){ 69, 68, 67, 86, 66, 71, 84, 82 }},
        //    /* 'g' */ { 71 , new List<int>(){ 82, 70, 86, 66, 78, 72, 89, 84 }},
        //    /* 'h' */ { 72 , new List<int>(){ 84, 71, 66, 78, 77, 74, 85, 89 }},
        //    /* 'j' */ { 74 , new List<int>(){ 89, 72, 78, 77, 188, 75, 73, 85 }},
        //    /* 'k' */ { 75 , new List<int>(){ 85, 74, 77, 188, 190, 76, 79, 73 }},
        //    /* 'l' */ { 76 , new List<int>(){ 73, 75, 188, 190, 191, 186, 80, 79 }},
        //    /* ';' */ { 186 , new List<int>(){ 79, 76, 190, 191, 222, 219, 80 }},
        //    /* '\''*/ { 222 , new List<int>(){ 80, 186, 191, 221, 219 }},
        //    /* 'z' */ { 90 , new List<int>(){ 65, 83, 88 }},
        //    /* 'x' */ { 88 , new List<int>(){ 90, 65, 83, 68, 67 }},
        //    /* 'c' */ { 67 , new List<int>(){ 88, 83, 68, 70, 86 }},
        //    /* 'v' */ { 86 , new List<int>(){ 67, 68, 70, 71, 66 }},
        //    /* 'b' */ { 66 , new List<int>(){ 86, 70, 71, 72, 78 }},
        //    /* 'n' */ { 78 , new List<int>(){ 66, 71, 72, 74, 77 }},
        //    /* 'm' */ { 77 , new List<int>(){ 78, 72, 74, 75, 188 }},
        //    /* '<' */ { 188 , new List<int>(){ 77, 74, 75, 76, 190 }},
        //    /* '>' */ { 190 , new List<int>(){ 188, 75, 76, 186, 191 }},
        //    /* '?' */ { 191 , new List<int>(){ 190, 76, 186, 222 }},
        //};

        public async Task OrganizeKeywords(CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;
                    var records = dbContainer.AudioBookSet.GetRecordsAllWithCache();

                    var sw = new Stopwatch();
                    sw.Start();

                    var symbolicDistancePlugin = Context.I.ComponentFactory.CreateActual<ISymbolicDistance>();

                    var allGenres = records.SelectMany(item => item.GetGenres())
                        .Where(item=>item.Length > 3)
                        .GroupBy(item => item)
                        .ToDictionary(item => item.Key, item => item.Count());

                    var topMost = allGenres.Where(item => item.Value >= 5 && item.Key.Length > 3).OrderByDescending(item => item.Value)
                        .ToDictionary(item => item.Key, item => item.Value);

                    symbolicDistancePlugin.SetData(topMost.Select(item => Tuple.Create(item.Key, item.Key)).ToList());

                    foreach (var genre in allGenres)
                    {
                        if (!topMost.ContainsKey(genre.Key))
                        {
                            //var candidates = symbolicDistancePlugin.Search(genre.Key).OrderByDescending(item=>item.Cost).ToArray();

                            KeyValuePair<string, int> result = new KeyValuePair<string, int>();
                            int minDistance = Int32.MaxValue;

                            foreach (KeyValuePair<string, int> topMostKey in topMost)
                            {
                                var distance = LevenshteinDistance.Compute(topMostKey.Key, genre.Key);
                                if (distance < 3)
                                {
                                    if (distance < minDistance)
                                    {
                                        result = topMostKey;
                                        minDistance = distance;
                                    }
                                    else if (distance == minDistance && topMostKey.Value > result.Value)
                                    {
                                        result = topMostKey;
                                    }
                                }
                            }

                            if (result.Value > 0)
                            {
                                Debug.WriteLine($"{genre.Key} => {result} ({minDistance})");
                            }
                        }
                    }

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

        protected virtual string CleanupHumanNameValue(string value, int maxLength)
        {
            return CleanupRecordValue(value, false, maxLength).TrimStart('©', '…', '—', ' ', '๖', 'ۣ', 'ۜ')
                .ChangeCase(Extensions.CaseTypes.AllWords, true, true);
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
                    record.Author = CleanupHumanNameValue(value, 500);
                    break;
                case "фамилия автора":
                    record.AuthorSurnameForParse = CleanupHumanNameValue(value, 250);
                    break;
                case "имя автора":
                    record.AuthorNameForParse = CleanupHumanNameValue(value, 250);
                    break;
                case "издательство":
                    record.Publisher =
                        CleanupRecordValue(value, false, 500).ChangeCase(Extensions.CaseTypes.FirstWord, true, true);
                    break;
                case "исполнитель":
                case "исполнители":
                case "запись и обработка":
                    record.Reader = CleanupHumanNameValue(value, 500);
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