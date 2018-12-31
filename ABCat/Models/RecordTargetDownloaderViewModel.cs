using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public class RecordTargetDownloaderViewModel : AsyncOperationViewModelBase
    {
        private readonly Func<IEnumerable<IAudioBook>> _getSelectedRecordsFunc;
        private readonly IRecordTargetDownloaderPlugin _recordTargetDownloaderPlugin;

        public RecordTargetDownloaderViewModel([NotNull] StatusBarStateViewModel statusBarStateModel,
            [NotNull] IRecordTargetDownloaderPlugin recordTargetDownloaderPlugin,
            [NotNull] Func<IReadOnlyCollection<IAudioBook>> getSelectedRecordsFunc)
            : base(statusBarStateModel)
        {
            _recordTargetDownloaderPlugin = recordTargetDownloaderPlugin;
            _getSelectedRecordsFunc = getSelectedRecordsFunc;
        }

        [UsedImplicitly]
        public ICommand DownloadRecordTargetCommand => CommandFactory.Get(
            async () => { await OnDownloadRecordTarget(); },
            () => CancellationTokenSource == null);

        public async Task OnDownloadRecordTarget()
        {
            CancellationTokenSource = new CancellationTokenSource();
            var targetRecords = new HashSet<string>(_getSelectedRecordsFunc().Select(item => item.Key));

            await _recordTargetDownloaderPlugin.DownloadRecordTarget(targetRecords, CancellationTokenSource.Token);

            Executor.OnUiThread(() =>
            {
                OnAsynOperationCompleted();
                CommandManager.InvalidateRequerySuggested();
                Context.I.EventAggregator.PublishOnUIThread(new RecordLoadedMessage());
            });
        }
    }
}