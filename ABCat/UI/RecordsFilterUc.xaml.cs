using System.Windows;
using System.Windows.Controls;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;

namespace ABCat.UI.WPF.UI
{
    /// <summary>
    ///     Interaction logic for FilterUc.xaml
    /// </summary>
    public partial class RecordsFilterUc : UserControl
    {
        // Using a DependencyProperty as the backing store for RecordsFilter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilteringLogicStandardProperty =
            DependencyProperty.Register("FilteringLogicStandard", typeof(IFilteringLogicPlugin),
                typeof(RecordsFilterUc), new PropertyMetadata(null));

        public RecordsFilterUc()
        {
            InitializeComponent();
        }

        public IFilteringLogicPlugin FilteringLogicStandard
        {
            get => (IFilteringLogicPlugin) GetValue(FilteringLogicStandardProperty);
            set
            {
                SetValue(FilteringLogicStandardProperty, value);
                DataContext = value;
            }
        }
    }
}