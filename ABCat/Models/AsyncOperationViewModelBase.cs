using System.Threading;
using System.Windows.Input;
using ABCat.Shared.Messages;
using ABCat.Shared.ViewModels;
using Caliburn.Micro;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public abstract class AsyncOperationViewModelBase : ViewModelBase, IHandle<ProgressMessage>
    {
        protected CancellationTokenSource CancellationTokenSource;

        protected AsyncOperationViewModelBase([NotNull] StatusBarStateViewModel statusBarStateModel)
        {
            StatusBarStateModel = statusBarStateModel;
            Context.I.EventAggregator.Subscribe(this);
        }

        public bool IsAsyncOperationExecuting => CancellationTokenSource != null;

        protected StatusBarStateViewModel StatusBarStateModel { get; set; }

        public void Handle(ProgressMessage message)
        {
            if (message.IsComplexProgress)
            {
                ReportProgressTotal(message.Completed, message.Total, message.Message);
            }
            else
            {
                ReportProgressSmall(message.Completed, message.Total, message.Message);
            }
        }

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