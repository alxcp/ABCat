﻿using System;
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
                    allGroups.Add(groupKey, siteParserPlugin.WebSiteId);
                }
            }

            var forReparse = _getSelectedItems().GroupBy(item => allGroups[item.GroupKey])
                .ToDictionary(item => item.Key, values => values.Select(item => item.Key).ToHashSet());

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