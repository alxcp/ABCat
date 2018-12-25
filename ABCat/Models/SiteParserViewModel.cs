using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;

namespace ABCat.UI.WPF.Models
{
    public class SiteParserViewModel : AsyncOperationViewModelBase
    {
        private readonly Func<IEnumerable<IAudioBook>> _getSelectedItems;
        private readonly AbCatViewModel _owner;
        private readonly IReadOnlyCollection<ISiteParserPlugin> _siteParserPlugins;

        public SiteParserViewModel(AbCatViewModel owner, Func<IEnumerable<IAudioBook>> getSelectedItems)
            : base(owner.StatusBarStateModel)
        {
            _owner = owner;
            _getSelectedItems = getSelectedItems;
            _siteParserPlugins = Context.I.ComponentFactory.GetCreators<ISiteParserPlugin>()
                .Select(item => item.GetInstance<ISiteParserPlugin>()).ToArray();
        }

        public ICommand DownloadRecordGroupsCommand =>
            CommandFactory.Get(async ()=> await OnDownloadRecordGroups(), () => CancellationTokenSource == null);

        public ICommand DownloadRecordsCommand =>
            CommandFactory.Get(async ()=> await OnDownloadRecords(), () => CancellationTokenSource == null);

        public ICommand ReparseCommand => CommandFactory.Get(async ()=> await Reparse(), ()=> CancellationTokenSource == null);

        private async Task Reparse()
        {
            CancellationTokenSource = new CancellationTokenSource();

            Dictionary<string, int> allGroups = new Dictionary<string, int>();

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
                var plugin = _siteParserPlugins.FirstOrDefault(item=>item.WebSiteId == audioBooks.Key);
                //await SiteParserPlugin.OrganizeKeywords(ReportProgressTotal, CancellationTokenSource.Token);

                if (plugin != null)
                {
                    await plugin.DownloadRecords(
                        audioBooks.Value,
                        PageSources.CacheOnly,
                        CancellationTokenSource.Token);

                    await Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>()
                        .UpdateCache(UpdateTypes.Values);
                }

                Executor.OnUiThread(() =>
                {
                    OnAsynOperationCompleted();
                    _owner.RefreshRecordsListData();
                });
            }
        }

        public async Task OnDownloadRecordGroups()
        {
            CancellationTokenSource = new CancellationTokenSource();

            foreach (var siteParserPlugin in _siteParserPlugins)
            {
                await siteParserPlugin.DownloadRecordGroups(null, CancellationTokenSource.Token);
            }

            Executor.OnUiThread(() =>
            {
                OnAsynOperationCompleted();
                _owner.RefreshRecordsListData();
            });
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

            Executor.OnUiThread(() =>
            {
                OnAsynOperationCompleted();
                _owner.RefreshRecordsListData();
            });
        }

        public async Task<string> DownloadRecordSourcePage(IAudioBook record, CancellationToken cancellationToken)
        {
            var groupKey = record.GroupKey;
            var plugin = _siteParserPlugins.FirstOrDefault(item=>item.GetGroupKeys(false).Contains(groupKey));

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