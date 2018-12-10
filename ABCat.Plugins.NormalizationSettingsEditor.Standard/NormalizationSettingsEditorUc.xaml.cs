using System.Windows.Input;

namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    public partial class NormalizationSettingsEditorUc
    {
        public NormalizationSettingsEditorUc()
        {
            InitializeComponent();
        }

        private void PossibleValuesLb_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var model = (NormalizationEditorViewModel) DataContext;
            if (model != null && !string.IsNullOrEmpty(model.CurrentPossibleValue))
            {
                model.ReplacementValue = model.CurrentPossibleValue;
            }
        }
    }
}