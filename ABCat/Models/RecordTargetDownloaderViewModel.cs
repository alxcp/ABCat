using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using ABCat.Shared;
using ABCat.Shared.Commands;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public class RecordTargetDownloaderViewModel : AsyncOperationViewModelBase
    {
        private readonly Func<IEnumerable<IAudioBook>> _getSelectedRecordsFunc;
        private readonly Action _recordDownloadedCallback;
        private readonly IRecordTargetDownloaderPlugin _recordTargetDownloaderPlugin;

        public RecordTargetDownloaderViewModel([NotNull] StatusBarStateViewModel statusBarStateModel,
            [NotNull] IRecordTargetDownloaderPlugin recordTargetDownloaderPlugin,
            [NotNull] Func<IEnumerable<IAudioBook>> getSelectedRecordsFunc,
            [NotNull] Action recordDownloadedCallback)
            : base(statusBarStateModel)
        {
            _recordTargetDownloaderPlugin = recordTargetDownloaderPlugin;
            _getSelectedRecordsFunc = getSelectedRecordsFunc;
            _recordDownloadedCallback = recordDownloadedCallback;
            DownloadRecordTargetCommand = new DelegateCommand(OnDownloadRecordTarget,
                obj => CancellationTokenSource == null);
        }

        [UsedImplicitly] public DelegateCommand DownloadRecordTargetCommand { get; }

        public void OnDownloadRecordTarget(object parameter)
        {
            CancellationTokenSource = new CancellationTokenSource();
            var targetRecords = new HashSet<string>(_getSelectedRecordsFunc().Select(item => item.Key));

            _recordTargetDownloaderPlugin.BeginDownloadRecordTargetAsync(targetRecords,
                ReportProgressSmall,
                ReportProgressTotal,
                DownloadRecordTargetAsyncCompleted,
                CancellationTokenSource.Token);
        }

        private void DownloadRecordTargetAsyncCompleted(Exception ex)
        {
            Executor.OnUiThread(() =>
            {
                OnAsynOperationCompleted();
                CommandManager.InvalidateRequerySuggested();

                if (ex == null) _recordDownloadedCallback();
                else throw ex;
            });
        }
    }
}