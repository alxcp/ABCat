namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    public partial class NormalizationSettingsUc
    {
        public NormalizationSettingsUc(NormalizationSettingsViewModel normalizationSettingsViewModel)
        {
            DataContext = normalizationSettingsViewModel;
            InitializeComponent();
        }
    }
}