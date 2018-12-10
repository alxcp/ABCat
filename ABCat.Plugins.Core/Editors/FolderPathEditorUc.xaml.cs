using System.Windows;
using System.Windows.Forms;

namespace ABCat.Core.Editors
{
    /// <summary>
    ///     Interaction logic for FolderPathEditor.xaml
    /// </summary>
    public partial class FolderPathEditorUc
    {
        // Using a DependencyProperty as the backing store for FolderPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FolderPathProperty =
            DependencyProperty.Register("FolderPath", typeof(string), typeof(FolderPathEditorUc),
                new PropertyMetadata(null));

        public FolderPathEditorUc()
        {
            InitializeComponent();
        }

        public string FolderPath
        {
            get => (string) GetValue(FolderPathProperty);
            set => SetValue(FolderPathProperty, value);
        }

        private void SelectFolderBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog
            {
                SelectedPath = FolderPath,
                ShowNewFolderButton = true
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                FolderPath = fbd.SelectedPath;
            }
        }
    }
}