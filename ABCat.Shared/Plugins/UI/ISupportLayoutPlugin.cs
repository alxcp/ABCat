using System.Windows;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.UI
{
    public interface IControlPlugin : IExtComponent
    {
        FrameworkElement Control { get; }

        void RestoreLayout();
        void StoreLayout();
    }
}