using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ABCat.Shared;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace ABCat.Core.Editors
{
    public class EnumEditor : ITypeEditor
    {
        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            var fcmb = new ComboBox();
            var binding = new Binding("Value")
            {
                Source = propertyItem,
                ValidatesOnExceptions = true,
                ValidatesOnDataErrors = true,
                Converter = new LocalizationValueConverter {TargetType = propertyItem.PropertyType},
                Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
            };

            BindingOperations.SetBinding(fcmb, Selector.SelectedValueProperty, binding);
            fcmb.ItemsSource =
                Enum.GetValues(propertyItem.PropertyType).Cast<Enum>().Select(item => item.GetDescription());
            return fcmb;
        }
    }
}