using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace ABCat.Plugins.FilteringLogics.Standard
{
    public class FilteredComboBoxEditor : ITypeEditor
    {
        private static readonly Dictionary<string, ObservableCollection<string>> Sources =
            new Dictionary<string, ObservableCollection<string>>();

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            if (Sources.TryGetValue(propertyItem.PropertyDescriptor.Name, out var itemsSource))
            {
                var fcmb = new AutoFilterCmb();
                var binding = new Binding("Value")
                {
                    Source = propertyItem,
                    ValidatesOnExceptions = true,
                    ValidatesOnDataErrors = true,
                    Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                BindingOperations.SetBinding(fcmb, ComboBox.TextProperty, binding);
                fcmb.ItemsSource = itemsSource;
                return fcmb;
            }
            else
            {
                var textBox = new TextBox();
                var binding = new Binding("Value")
                {
                    Source = propertyItem,
                    ValidatesOnExceptions = true,
                    ValidatesOnDataErrors = true,
                    Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                BindingOperations.SetBinding(textBox, TextBox.TextProperty, binding);
                return textBox;
            }
        }

        public static void Register(string propertyItemName, ObservableCollection<string> itemsSource)
        {
            Sources.Add(propertyItemName, itemsSource);
        }
    }
}