using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using ABCat.Shared;
using ABCat.Shared.Plugins.UI;
using Component.Infrastructure;
using Component.Infrastructure.Factory;

namespace ABCat.Plugins.BrowserWindow.WPFWebBrowser
{
    /// <summary>
    ///     Interaction logic for WPFWebBrowser.xaml
    /// </summary>
    [SingletoneComponentInfo("1.0")]
    public partial class WpfWebBrowserWindowPlugin : IBrowserWindowPlugin
    {
        private readonly WebBrowser _webBrowser;

        private string _previousFileName;

        public WpfWebBrowserWindowPlugin()
        {
            InitializeComponent();

            if (_webBrowser == null) _webBrowser = new WebBrowser();
            _webBrowser.ScriptErrorsSuppressed = true;
            SuppressScriptErrorsOnly(_webBrowser);
            WebBrowserContainer.Child = _webBrowser;
        }

        public Config Config { get; set; }
        public bool IsDisplayed => Visibility == Visibility.Visible;
        public event EventHandler WindowClosed;

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        public void Display()
        {
            Show();
        }

        public void Dispose()
        {
            _webBrowser.Dispose();
            Disposed.Fire(this);
        }

        public void RestoreLayout()
        {
        }

        public FrameworkElement Control => this;

        public void ShowRecordPage(string title, string pageHtml)
        {
            if (!string.IsNullOrEmpty(_previousFileName) && File.Exists(_previousFileName))
                File.Delete(_previousFileName);
            _previousFileName = null;

            pageHtml = "<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head>" +
                       pageHtml;
            _previousFileName = Path.GetTempFileName() + ".html";
            File.WriteAllText(_previousFileName, pageHtml);
            _webBrowser.Navigate(_previousFileName);
            Title = title;
        }

        public void StoreLayout()
        {
        }

        public event EventHandler Disposed;

        private void SuppressScriptErrorsOnly(WebBrowser browser)
        {
            browser.DocumentCompleted += (sender, e) =>
            {
                var document = ((WebBrowser) sender).Document;
                if (document != null && document.Window != null)
                {
                    document.Window.Error += (senderError, eError) => { eError.Handled = true; };
                }
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            WindowClosed?.Invoke(this, e);
        }
    }
}