using System;
using ABCat.Shared.Plugins.UI;
using ABCat.Shared.ViewModels;

namespace ABCat.UI.WPF.Models
{
    public sealed class NormalizationSettingsEditorViewModel : ViewModelBase, IDisposable
    {
        private bool _isActive;

        public NormalizationSettingsEditorViewModel()
        {
            NormalizationSettingsEditorPlugin =
                Context.I.ComponentFactory.CreateActual<INormalizationSettingsEditorPlugin>();
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value.Equals(_isActive)) return;
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public INormalizationSettingsEditorPlugin NormalizationSettingsEditorPlugin { get; }

        public void Dispose()
        {
            NormalizationSettingsEditorPlugin?.Dispose();
        }
    }
}