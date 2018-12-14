using System.Threading;
using System.Windows.Input;
using ABCat.Shared.ViewModels;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public abstract class AsyncOperationViewModelBase : ViewModelBase
    {
        protected CancellationTokenSource CancellationTokenSource;

        protected AsyncOperationViewModelBase([NotNull] StatusBarStateViewModel statusBarStateModel)
        {
            StatusBarStateModel = statusBarStateModel;
        }

        public bool IsAsyncOperationExecuting => CancellationTokenSource != null;

        protected StatusBarStateViewModel StatusBarStateModel { get; set; }

        public void CancelAsyncOperation()
        {
            var cts = CancellationTokenSource;
            cts?.Cancel();
        }

        protected void OnAsynOperationCompleted()
        {
            CancellationTokenSource = null;
            ReportProgressSmall(-1, 0, null);
            ReportProgressTotal(-1, 0, null);
            CommandManager.InvalidateRequerySuggested();
            CommandFactory.UpdateAll();
        }

        protected void ReportProgressSmall(int current, int total, string message)
        {
            StatusBarStateModel.ProgressBarSmallMinimum = 0;
            StatusBarStateModel.ProgressBarSmallMaximum = total;
            StatusBarStateModel.ProgressBarSmallMessage = message;
            StatusBarStateModel.ProgressBarSmallValue = current;
        }

        protected void ReportProgressTotal(int current, int total, string message)
        {
            StatusBarStateModel.ProgressBarTotalMinimum = 0;
            StatusBarStateModel.ProgressBarTotalMaximum = total;
            StatusBarStateModel.ProgressBarTotalMessage = message;
            StatusBarStateModel.ProgressBarTotalValue = current;
        }
    }
}