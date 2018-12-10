using System.Windows;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace ABCat.Core.Editors
{
    public class FilePathEditor : ITypeEditor
    {
        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            var fpe = new FilePathEditorUc();
            var binding = new Binding("Value");
            binding.Source = propertyItem;
            binding.ValidatesOnExceptions = true;
            binding.ValidatesOnDataErrors = true;
            binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(fpe, FilePathEditorUc.FilePathProperty, binding);
            return fpe;
        }
    }
}