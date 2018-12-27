using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ABCat.Core;
using ABCat.Shared;
using ABCat.UI.WPF.Properties;

namespace ABCat.UI.WPF
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var context = ContextInitializer.Init();
            context.EventAggregator.Dispatcher = Dispatcher;
            TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            Current.DispatcherUnhandledException += CurrentDispatcherUnhandledException;
        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ProcessException((Exception) e.ExceptionObject);
        }

        private void CurrentDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ProcessException(e.Exception);
            e.Handled = true;
        }

        private void ProcessException(Exception ex)
        {
            var aex = ex as AbCatException;
            var targetException = ex;
            var type = ExceptionTypes.Stop;
            var log = true;

            if (aex != null)
            {
                targetException = aex.InnerException ?? aex;
                type = aex.ExceptionType;
                log = aex.Log;
            }

            if (log) Context.I.Logger.Error(targetException, targetException.Message);

            MessageBox.Show(ex.Message + (log ? "\r\nБолее подробная информация в файле лога ошибок." : null));

            switch (type)
            {
                case ExceptionTypes.Info:
                    break;
                case ExceptionTypes.Warning:
                    break;
                case ExceptionTypes.Stop:
                    Dispatcher.CheckAccess(() => Current.Shutdown());
                    break;
            }
        }

        private void TaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception targetException = e.Exception;
            if (e.Exception.InnerException != null) targetException = e.Exception.InnerException;

            ProcessException(targetException);
        }
    }
}