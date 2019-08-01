using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ABCat.Shared;
using Component.Infrastructure;
using Microsoft.Win32;

namespace ABCat.UI.WPF
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindowLoaded;

#if DEBUG
            AddHandler(KeyDownEvent, new KeyEventHandler((e, w) =>
            {
                if (w.Key == Key.F12)
                {
                    var sfd = new SaveFileDialog {RestoreDirectory = true, DefaultExt = ".png", AddExtension = true};

                    if (sfd.ShowDialog().Value)
                    {
                        File.WriteAllBytes(sfd.FileName, this.ToBitmap());
                    }
                }
            }), true);

#endif
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            CatalogViewerUc.StoreLayouts();
            Saveable.SaveAll();
            Application.Current.Shutdown();
        }

        private async void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                await CatalogViewerUc.Init();
                CatalogViewerUc.RestoreLayouts();
            }
        }
    }
}