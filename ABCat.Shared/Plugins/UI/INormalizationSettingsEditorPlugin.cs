using System.Collections.Generic;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Shared.Plugins.UI
{
    public interface INormalizationSettingsEditorPlugin : IControlPlugin
    {
        bool IsOnUpdate { get; }
        IEnumerable<IAudioBook> TargetRecordsForEdit { get; set; }

        Task RefreshData();
    }
}