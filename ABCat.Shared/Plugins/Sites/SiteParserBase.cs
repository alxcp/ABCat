using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task DownloadRecordGroups(HashSet<string> recordGroupsKeys,
            ProgressCallback smallProgressCallback, ProgressCallback totalProgressCallback,
            CancellationToken cancellationToken)
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

                    foreach (
                        var group in
                        groups.Where(item => (DateTime.Now - item.LastUpdate).TotalDays > groupActualityPeriod))
                    {
                        try
                        {
                            totalProgressCallback(z, groups.Count(), "{0} из {1}".F(z, groups.Count()));
                            DownloadRecordGroup(dbContainer, group, smallProgressCallback, cancellationToken);
                            if (cancellationToken.IsCancellationRequested) break;
                            dbContainer.SaveChanges();
                            z++;
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
            }, cancellationToken);
        }

        public async Task DownloadRecords(HashSet<string> recordsKeys, PageSources pageSource,
            ProgressCallback smallProgressCallback, ProgressCallback totalProgressCallback,
            CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                var recordActualityPeriod = mainConfig.RecordActualityPeriod;

                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
                    List<IAudioBook> records;

                    if (recordsKeys == null)
                    {
                        if (pageSource == PageSources.CacheOnly)
                        {
                            records = dbContainer.AudioBookSet.GetRecordsAll().ToList();
                        }
                        else
                        {
                            records =
                                dbContainer.AudioBookSet.GetRecordsUpdatedBefore(
                                    DateTime.Now.Subtract(TimeSpan.FromDays(recordActualityPeriod))).ToList();
                        }
                    }
                    else
                    {
                        records = dbContainer.AudioBookSet.GetRecordsByKeys(recordsKeys).ToList();
                    }

                    var sw = new Stopwatch();
                    sw.Start();

                    var normalizerPlugin =
                        Context.I.ComponentFactory.GetCreators<INormalizationLogicPlugin>()
                            .First()
                            .GetInstance<INormalizationLogicPlugin>();

                    var waitingForSave = new List<IAudioBook>();

                    for (var z = 0; z < records.Count; z++)
                    {
                        try
                        {
                            var record = records[z];
                            totalProgressCallback(z, records.Count, "{0} из {1}".F(z, records.Count()));
                            DownloadRecord(dbContainer, record, pageSource, smallProgressCallback,
                                cancellationToken);
                            record.LastUpdate = DateTime.Now;
                            waitingForSave.Add(record);

                            if (record.Created == default(DateTime))
                            {
                                record.Created = DateTime.Now;
                            }

                            if (cancellationToken.IsCancellationRequested) break;
                            if (sw.Elapsed > TimeSpan.FromSeconds(30) || z == records.Count - 1)
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

        public async Task<string> DownloadRecordSourcePage(IAudioBook audioBook, CancellationToken cancellationToken)
        {
            return await Task.Factory.StartNew(() => GetRecordSourcePageString(audioBook, PageSources.CacheOrWeb, cancellationToken), cancellationToken);
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfigs);

        public void Dispose()
        {
            Disposed.Fire(this);
        }

        public event EventHandler Disposed;

        public abstract IEnumerable<IAudioBookGroup> GetAllRecordGroups(IDbContainer container);

        protected virtual string CleanupRecordValue(string value, bool allowMultiLine, int maxLenght)
        {
            var result = allowMultiLine ? value : value.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)[0];

            do
            {
                value = result;
                result = value.Trim().Trim('\r', '\n', ':', '-', '=');
            } while (result != value);

            if (maxLenght > 0 && result.Length > maxLenght)
            {
                result = result.Substring(0, maxLenght - 1) + "…";
            }

            return result;
        }

        /// <summary>
        ///     Очистить страницу описания торрента от лишних элементов (заголовок, меню, скрипты и др.)
        /// </summary>
        /// <param name="document">Документ</param>
        protected virtual void ClearRecordPage(HtmlDocument document)
        {
            foreach (var script in document.GetNodes("script", "type", str => true).ToArray())
            {
                script.ParentNode.RemoveChild(script);
            }
        }

        protected abstract void DownloadRecord(IDbContainer db, IAudioBook record, PageSources pageSource,
            ProgressCallback progressCallback, CancellationToken cancellationToken);

        protected abstract void DownloadRecordGroup(IDbContainer db, IAudioBookGroup recordGroup,
            ProgressCallback progressCallback, CancellationToken cancellationToken);

        protected abstract string GetRecordSourcePageString(IAudioBook audioBook, PageSources pageSource,
            CancellationToken cancellationToken);

        protected abstract void ParseRecord(IDbContainer db, IAudioBook record, string postBodyHtml);
    }
}