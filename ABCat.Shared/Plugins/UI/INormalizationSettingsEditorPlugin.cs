using System.Collections.Generic;
using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Shared.Plugins.UI
{
    public interface INormalizationSettingsEditorPlugin : IControlPlugin
    {
        bool IsOnUpdate { get; }
        IEnumerable<IAudioBook> TargetRecordsForEdit { get; set; }

        void BeginRefreshDataAsync();
    }
}