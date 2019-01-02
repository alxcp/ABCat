using System.IO;
using System.Threading.Tasks;
using ABCat.UI.WPF.Models;
using Component.Infrastructure;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace ABCat.UI.WPF.UI
{
    public partial class CatalogViewerUc
    {
        private AbCatViewModel _abCatModel;

        public CatalogViewerUc()
        {
            InitializeComponent();
        }

        public async Task Init()
        {
            _abCatModel = new AbCatViewModel();
            DataContext = _abCatModel;
            FilterUc.FilteringLogicStandard = _abCatModel.Filter;
            StatusBarStateUc.DataContext = _abCatModel.StatusBarStateModel;
            await _abCatModel.RefreshAll();
        }

        public void RestoreLayouts()
        {
            _abCatModel.RecordsListUc?.RestoreLayout();
            _abCatModel.NormalizationSettingsEditorModel.NormalizationSettingsEditorPlugin?.RestoreLayout();

            //var dockingManagerLayout = ABCatConfig ["CatalogViewerUc.DockingManager"] as byte[];
            if (Config.TryLoadLayout("CatalogViewerUc_DockingManager", out var layout))
            {
                using (var memoryStream = new MemoryStream(layout))
                {
                    var layoutSerializer = new XmlLayoutSerializer(DockingManager);
                    layoutSerializer.Deserialize(memoryStream);
                }
            }
        }

        public void StoreLayouts()
        {
            if (_abCatModel != null)
            {
                _abCatModel.RecordsListUc?.StoreLayout();
                _abCatModel.NormalizationSettingsEditorModel.NormalizationSettingsEditorPlugin?.StoreLayout();
            }

            using (var memoryStream = new MemoryStream())
            {
                new XmlLayoutSerializer(DockingManager).Serialize(memoryStream);
                Config.SaveLayout("CatalogViewerUc_DockingManager", memoryStream.ToArray());
            }
        }
    }
}