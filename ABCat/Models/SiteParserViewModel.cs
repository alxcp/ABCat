using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            ReparseCommand = new DelegateCommand(Reparse, IsCanReparse);
            DownloadRecordsCommand = new DelegateCommand(OnDownloadRecords, CanDownloadRecords);
            DownloadRecordGroupsCommand = new DelegateCommand(OnDownloadRecordGroups, CanDownloadRecordGroups);
        }

        public DelegateCommand DownloadRecordGroupsCommand { get; }

        public DelegateCommand DownloadRecordsCommand { get; }

        public DelegateCommand ReparseCommand { get; }

        public ISiteParserPlugin SiteParserPlugin { get; set; }

        private bool IsCanReparse(object arg)
        {
            return CancellationTokenSource == null;
        }

        private void Reparse(object obj)
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

            //foreach (var record in _getSelectedItems())
            //{
            //    SiteParserPlugin.BeginDownloadRecordSourcePageAsync(record, (s, ex) => { },
            //        CancellationTokenSource.Token);
            //}
        }

        public bool CanDownloadRecordGroups(object parameter)
        {
            return CancellationTokenSource == null;
        }

        public bool CanDownloadRecords(object parameter)
        {
            return CancellationTokenSource == null;
        }

        public void OnDownloadRecordGroups(object parameter)
        {
            CancellationTokenSource = new CancellationTokenSource();

            SiteParserPlugin.BeginDownloadRecordGroupsAsync(null,
                ReportProgressSmall,
                ReportProgressTotal,
                DownloadRecordGroupsAsyncCompleted,
                CancellationTokenSource.Token);
        }

        public void OnDownloadRecords(object parameter)
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