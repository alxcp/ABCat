using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public abstract class SiteParserBase : ISiteParserPlugin
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
                    using (var dbContainer = Context.I.CreateDbContainer(false))
                    {
                        var ws = dbContainer.WebSiteSet.GetWebSitesAll()
                            .FirstOrDefault(item => item.SiteParserPluginName.Compare(GetType().Name));
                        if (ws == null)
                        {
                            ws = dbContainer.WebSiteSet.CreateWebSite();
                            ws.SiteParserPluginName = GetType().Name;
                            dbContainer.WebSiteSet.AddWebSite(ws);
                            ws = dbContainer.WebSiteSet.GetWebSitesAll()
                                .First(item => item.SiteParserPluginName.Compare(GetType().Name));
                        }

                        _webSiteId = ws.Id;
                    }
                }

                return _webSiteId;
            }
        }

        public HashSet<string> GetGroupKeys(bool forceRefresh)
        {
            if (_groupKeys == null || forceRefresh)
            {
                var webSiteId = WebSiteId;
                using (var dbContainer = Context.I.CreateDbContainer(false))
                {
                    _groupKeys = dbContainer.AudioBookGroupSet.GetRecordGroupsAll()
                        .Where(item => item.WebSiteId == webSiteId).Select(item => item.Key).ToHashSet();
                }
            }

            return _groupKeys;
        }

        public abstract Uri GetRecordPageUrl(IAudioBook record);

        public async Task DownloadRecordGroups(HashSet<string> recordGroupsKeys, CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                var groupActualityPeriod = mainConfig.GroupActualityPeriod;
                var z = 0;
                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
                    IAudioBookGroup[] groups;
                    if (recordGroupsKeys == null) groups = GetAllRecordGroups(dbContainer).ToArray();
                    else
                        groups =
                            GetAllRecordGroups(dbContainer)
                                .Where(item => recordGroupsKeys.Contains(item.Key))
                                .ToArray();

                    foreach (var group in groups.Where(item => (DateTime.Now - item.LastUpdate).TotalDays > groupActualityPeriod))
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

        public async Task DownloadRecords(HashSet<string> recordsKeys, PageSources pageSource, CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                var recordActualityPeriod = mainConfig.RecordActualityPeriod;

                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
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
                var recordActualityPeriod = mainConfig.RecordActualityPeriod;

                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
                    List<IAudioBook> records = dbContainer.AudioBookSet.GetRecordsAll().ToList();

                    var sw = new Stopwatch();
                    sw.Start();

                    var symbolicDistancePlugin = Context.I.ComponentFactory.GetActualCreator<ISymbolicDistance>();

                    var allGenres = records.SelectMany(item => item.Genre.Split(',')).Select(item=>item.Trim()).GroupBy(item => item)
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

            result = HtmlSpecCharRegex.Replace(result, RegexReadTerm);

            return result;
        }

        static string RegexReadTerm(Match m)
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
    }
}