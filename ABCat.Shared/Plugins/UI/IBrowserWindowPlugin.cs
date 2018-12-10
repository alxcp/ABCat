using System;

namespace ABCat.Shared.Plugins.UI
{
    public interface IBrowserWindowPlugin : IControlPlugin
    {
        bool IsDisplayed { get; }
        event EventHandler WindowClosed;

        void Display();
        void ShowRecordPage(string title, string recordPage);
    }
}