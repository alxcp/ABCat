using System.Threading.Tasks;
using Caliburn.Micro;
using Action = System.Action;

// ReSharper disable once CheckNamespace
public static class Executor
{
    /// <summary>
    ///     Indicates whether or not the framework is in design-time mode.
    /// </summary>
    public static bool InDesignMode => PlatformProvider.Current.InDesignMode;

    /// <summary>
    ///     Executes the action on the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public static void BeginOnUiThread(this Action action)
    {
        PlatformProvider.Current.BeginOnUIThread(action);
    }

    /// <summary>
    ///     Executes the action on the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public static Task OnUiThreadAsync(this Action action)
    {
        return PlatformProvider.Current.OnUIThreadAsync(action);
    }

    /// <summary>
    ///     Executes the action on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public static void OnUiThread(this Action action)
    {
        PlatformProvider.Current.OnUIThread(action);
    }
}