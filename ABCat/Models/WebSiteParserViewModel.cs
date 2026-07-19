using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.Plugins.Catalog.Normalizing;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;

namespace ABCat.UI.WPF.Models
{
    public class WebSiteParserViewModel : AsyncOperationViewModelBase
    {
        private readonly Func<IEnumerable<IAudioBook>> _getSelectedItems;
        private readonly AbCatViewModel _owner;
        private readonly IReadOnlyCollection<IWebSiteParserPlugin> _siteParserPlugins;

        public WebSiteParserViewModel(AbCatViewModel owner, Func<IReadOnlyCollection<IAudioBook>> getSelectedItems)
            : base(owner.StatusBarStateModel)
        {
            _owner = owner;
            _getSelectedItems = getSelectedItems;
            _siteParserPlugins = Context.I.ComponentFactory.GetCreators<IWebSiteParserPlugin>()
                .Select(item => item.GetInstance<IWebSiteParserPlugin>()).ToArray();
        }

        public ICommand DownloadRecordGroupsCommand =>
            CommandFactory.Get(async () => await OnDownloadRecordGroups(), () => CancellationTokenSource == null);

        public ICommand DownloadRecordsCommand =>
            CommandFactory.Get(async () => await OnDownloadRecords(), () => CancellationTokenSource == null);

        public ICommand ReparseCommand =>
            CommandFactory.Get(async () => await Reparse(), () => CancellationTokenSource == null);

        private async Task Reparse()
        {
            CancellationTokenSource = new CancellationTokenSource();

            var allGroups = new Dictionary<string, int>();

            foreach (var siteParserPlugin in _siteParserPlugins)
            {
                foreach (var groupKey in siteParserPlugin.GetGroupKeys(false))
                {
                    allGroups[groupKey] = siteParserPlugin.WebSiteId;
                }
            }

            var forReparse = new Dictionary<int, HashSet<string>>();

            foreach (var record in _getSelectedItems())
            {
                // A record whose group is no longer served by an enabled parser is skipped
                // instead of aborting the whole run.
                if (record.GroupKey == null || !allGroups.TryGetValue(record.GroupKey, out var webSiteId))
                    continue;

                if (!forReparse.TryGetValue(webSiteId, out var keys))
                {
                    keys = new HashSet<string>();
                    forReparse.Add(webSiteId, keys);
                }

                keys.Add(record.Key);
            }

            if (!forReparse.Any())
            {
                // Nothing selected means "reprocess the whole catalog": a null key set makes
                // each parser re-read every record of its own web site from the page cache,
                // so the pages are re-parsed without being downloaded again.
                foreach (var siteParserPlugin in _siteParserPlugins)
                {
                    forReparse[siteParserPlugin.WebSiteId] = null;
                }
            }

            foreach (var audioBooks in forReparse)
            {
                var plugin = _siteParserPlugins.FirstOrDefault(item => item.WebSiteId == audioBooks.Key);

                if (plugin != null)
                {
                    await plugin.DownloadRecords(
                        audioBooks.Value,
                        PageSources.CacheOnly,
                        CancellationTokenSource.Token);

                    await Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>()
                        .UpdateCache(UpdateTypes.Values);
                }
            }

            var normalizers = Context.I.ComponentFactory.CreateAll<IRecordsTagNormalizer>();

            foreach (var recordsTagNormalizer in normalizers)
            {
                await recordsTagNormalizer.Normalize(_getSelectedItems().Select(item => item.Key).ToArray(),
                    CancellationTokenSource.Token);
            }

            Executor.OnUiThread(OnAsynOperationCompleted);
        }

        public async Task OnDownloadRecordGroups()
        {
            CancellationTokenSource = new CancellationTokenSource();

            foreach (var siteParserPlugin in _siteParserPlugins)
            {
                await siteParserPlugin.DownloadRecordGroups(null, CancellationTokenSource.Token);
            }

            Executor.OnUiThread(OnAsynOperationCompleted);
        }

        public async Task OnDownloadRecords()
        {
            CancellationTokenSource = new CancellationTokenSource();

            foreach (var siteParserPlugin in _siteParserPlugins)
            {
                await siteParserPlugin.DownloadRecords(null,
                    PageSources.WebOnly,
                    CancellationTokenSource.Token);
            }

            Executor.OnUiThread(OnAsynOperationCompleted);
        }

        public async Task<string> DownloadRecordSourcePage(IAudioBook record, CancellationToken cancellationToken)
        {
            var groupKey = record.GroupKey;
            var plugin = _siteParserPlugins.FirstOrDefault(item => item.GetGroupKeys(false).Contains(groupKey));

            if (plugin != null)
            {
                return await plugin.DownloadRecordSourcePage(record, cancellationToken);
            }

            return string.Empty;
        }

        public Uri GetRecordPageUrl(IAudioBook record)
        {
            var groupKey = record.GroupKey;
            var plugin = _siteParserPlugins.FirstOrDefault(item => item.GetGroupKeys(false).Contains(groupKey));
            return plugin?.GetRecordPageUrl(record);
        }
    }
}