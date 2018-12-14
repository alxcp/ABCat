using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using ABCat.Shared;
using ABCat.Shared.Commands;
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
            CommandFactory.Get(OnDownloadRecordGroups, () => CancellationTokenSource == null);

        public ICommand DownloadRecordsCommand =>
            CommandFactory.Get(OnDownloadRecords, () => CancellationTokenSource == null);

        public ICommand ReparseCommand => CommandFactory.Get(Reparse, ()=> CancellationTokenSource == null);

        public ISiteParserPlugin SiteParserPlugin { get; set; }

        private void Reparse()
        {
            CancellationTokenSource = new CancellationTokenSource();

            var keys = _getSelectedItems().Select(item => item.Key).Distinct().ToHashSet();

            SiteParserPlugin.BeginDownloadRecordsAsync(
                keys,
                PageSources.CacheOrWeb,
                ReportProgressSmall,
                ReportProgressTotal,
                DownloadRecordsAsyncCompleted,
                CancellationTokenSource.Token);
        }

        public void OnDownloadRecordGroups()
        {
            CancellationTokenSource = new CancellationTokenSource();

            SiteParserPlugin.BeginDownloadRecordGroupsAsync(null,
                ReportProgressSmall,
                ReportProgressTotal,
                DownloadRecordGroupsAsyncCompleted,
                CancellationTokenSource.Token);
        }

        public void OnDownloadRecords()
        {
            CancellationTokenSource = new CancellationTokenSource();

            SiteParserPlugin.BeginDownloadRecordsAsync(null,
                PageSources.WebOnly,
                ReportProgressSmall,
                ReportProgressTotal,
                DownloadRecordsAsyncCompleted,
                CancellationTokenSource.Token);
        }

        private void DownloadRecordGroupsAsyncCompleted(Exception obj)
        {
            Executor.OnUiThread(() =>
            {
                if (obj == null)
                {
                    OnAsynOperationCompleted();
                    _owner.RefreshRecordsListData();
                }
            });
        }

        private void DownloadRecordsAsyncCompleted(Exception obj)
        {
            Executor.OnUiThread(() =>
            {
                if (obj == null)
                {
                    OnAsynOperationCompleted();
                    _owner.RefreshRecordsListData();
                }
            });
        }
    }
}