using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ABCat.Shared;
using ABCat.Shared.Commands;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;

namespace ABCat.UI.WPF.Models
{
    public class SiteParserViewModel : AsyncOperationViewModelBase
    {
        private readonly Func<IEnumerable<IAudioBook>> _getSelectedItems;
        private readonly AbCatViewModel _owner;

        public SiteParserViewModel(AbCatViewModel owner, ISiteParserPlugin siteParserPlugin,
            Func<IEnumerable<IAudioBook>> getSelectedItems)
            : base(owner.StatusBarStateModel)
        {
            _owner = owner;
            _getSelectedItems = getSelectedItems;
            SiteParserPlugin = siteParserPlugin;
        }

        public ICommand DownloadRecordGroupsCommand =>
            CommandFactory.Get(async ()=> await OnDownloadRecordGroups(), () => CancellationTokenSource == null);

        public ICommand DownloadRecordsCommand =>
            CommandFactory.Get(async ()=> await OnDownloadRecords(), () => CancellationTokenSource == null);

        public ICommand ReparseCommand => CommandFactory.Get(async ()=> await Reparse(), ()=> CancellationTokenSource == null);

        public ISiteParserPlugin SiteParserPlugin { get; set; }

        private async Task Reparse()
        {
            CancellationTokenSource = new CancellationTokenSource();

            var keys = _getSelectedItems().Select(item => item.Key).Distinct().ToHashSet();

            //await SiteParserPlugin.OrganizeKeywords(ReportProgressTotal, CancellationTokenSource.Token);

            await SiteParserPlugin.DownloadRecords(
                keys,
                PageSources.CacheOrWeb,
                ReportProgressSmall,
                ReportProgressTotal,
                CancellationTokenSource.Token);

            await Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>()
                .UpdateCache(UpdateTypes.Values);

            Executor.OnUiThread(() =>
            {
                OnAsynOperationCompleted();
                _owner.RefreshRecordsListData();
            });
        }

        public async Task OnDownloadRecordGroups()
        {
            CancellationTokenSource = new CancellationTokenSource();

            await SiteParserPlugin.DownloadRecordGroups(null,
                ReportProgressSmall,
                ReportProgressTotal,
                CancellationTokenSource.Token);

            Executor.OnUiThread(() =>
            {
                OnAsynOperationCompleted();
                _owner.RefreshRecordsListData();
            });
        }

        public async Task OnDownloadRecords()
        {
            CancellationTokenSource = new CancellationTokenSource();

            await SiteParserPlugin.DownloadRecords(null,
                PageSources.WebOnly,
                ReportProgressSmall,
                ReportProgressTotal,
                CancellationTokenSource.Token);

            Executor.OnUiThread(() =>
            {
                OnAsynOperationCompleted();
                _owner.RefreshRecordsListData();
            });
        }
    }
}