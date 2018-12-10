using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace ABCat.Shared
{
    public class ModernWindowBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Style = (Style) Application.Current.Resources["ModernWindowStyle"];
            AssociatedObject.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            AssociatedObject.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand,
                OnMaximizeWindow, OnCanResizeWindow));
            AssociatedObject.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                OnMinimizeWindow, OnCanMinimizeWindow));
            AssociatedObject.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
                OnRestoreWindow,
                OnCanResizeWindow));
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.CommandBindings.Remove(
                new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            AssociatedObject.CommandBindings.Remove(new CommandBinding(SystemCommands.MaximizeWindowCommand,
                OnMaximizeWindow, OnCanResizeWindow));
            AssociatedObject.CommandBindings.Remove(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                OnMinimizeWindow, OnCanMinimizeWindow));
            AssociatedObject.CommandBindings.Remove(new CommandBinding(SystemCommands.RestoreWindowCommand,
                OnRestoreWindow, OnCanResizeWindow));
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = AssociatedObject.ResizeMode != ResizeMode.NoResize;
        }

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = AssociatedObject.ResizeMode == ResizeMode.CanResize ||
                           AssociatedObject.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void OnCloseWindow(object target, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(AssociatedObject);
        }

        private void OnMaximizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(AssociatedObject);
        }

        private void OnMinimizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(AssociatedObject);
        }

        private void OnRestoreWindow(object target, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(AssociatedObject);
        }
    }
}