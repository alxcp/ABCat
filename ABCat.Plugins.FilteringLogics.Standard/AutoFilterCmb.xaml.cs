namespace ABCat.Plugins.FilteringLogics.Standard
{
    /// <summary>
    ///     Interaction logic for AutoFilterCmb.xaml
    /// </summary>
    public partial class AutoFilterCmb
    {
        public AutoFilterCmb()
        {
            InitializeComponent();
            Items.Filter = FilterPredicate;
            IsEditable = true;
        }

        private bool FilterPredicate(object obj)
        {
            var text = obj as string;
            var filterText = Text;
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(filterText))
            {
                if (text.Contains(filterText))
                {
                    return true;
                }

                return false;
            }

            return string.IsNullOrEmpty(filterText);
        }
    }
}