using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace ABCat.Core.Editors
{
    /// <summary>
    ///     Interaction logic for FolderPathEditor.xaml
    /// </summary>
    public partial class FilePathEditorUc
    {
        // Using a DependencyProperty as the backing store for FolderPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register("FilePath", typeof(string), typeof(FilePathEditorUc),
                new PropertyMetadata(null));

        public FilePathEditorUc()
        {
            InitializeComponent();
        }

        public string FilePath
        {
            get => (string) GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        private void SelectFolderBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = string.IsNullOrEmpty(FilePath) ? null : Path.GetDirectoryName(FilePath),
                FileName = string.IsNullOrEmpty(FilePath) ? null : Path.GetFileName(FilePath)
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                FilePath = ofd.FileName;
            }
        }
    }
}