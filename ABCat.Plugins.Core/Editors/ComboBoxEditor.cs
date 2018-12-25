using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace ABCat.Core.Editors
{
    public class ComboBoxEditor : ITypeEditor
    {
        private static readonly Dictionary<string, ObservableCollection<object>> Sources =
            new Dictionary<string, ObservableCollection<object>>();

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            if (Sources.TryGetValue(propertyItem.PropertyDescriptor.Name, out var itemsSource))
            {
                var fcmb = new ComboBox();
                var binding = new Binding("Value");

                binding.Source = propertyItem;
                binding.ValidatesOnExceptions = true;
                binding.ValidatesOnDataErrors = true;
                binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
                BindingOperations.SetBinding(fcmb, Selector.SelectedValueProperty, binding);
                fcmb.ItemsSource = itemsSource;
                return fcmb;
            }

            throw new Exception(
                $"There is no registered data sources for property '{propertyItem.PropertyDescriptor.Name}'");
        }

        public static void Register([NotNull] string propertyItemName,
            [NotNull] ObservableCollection<object> itemsSource)
        {
            Sources[propertyItemName] = itemsSource;
        }
    }
}