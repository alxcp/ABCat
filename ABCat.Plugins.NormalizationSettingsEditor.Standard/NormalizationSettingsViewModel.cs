using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.UI;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    /// <summary>
    ///     Interaction logic for NormalizationSettingsUc.xaml
    /// </summary>
    [PerCallComponentInfo("1.0")]
    public sealed class NormalizationSettingsViewModel : INormalizationSettingsEditorPlugin, INotifyPropertyChanged
    {
        private NormalizationSettingsUc _control;
        private bool _editorVisible;
        private bool _isOnUpdate;
        private bool _viewerVisible;

        public NormalizationSettingsViewModel()
        {
            NormalizationViewerViewModel = new NormalizationViewerViewModel();
            NormalizationEditorViewModel = new NormalizationEditorViewModel();
            NormalizationEditorViewModel.PropertyChanged += NormalizationEditorViewModelPropertyChanged;
        }

        public Config Config { get; set; }

        public bool EditorVisible
        {
            get => _editorVisible;
            set
            {
                if (value.Equals(_editorVisible)) return;
                _editorVisible = value;
                OnPropertyChanged();
                if (value) ViewerVisible = false;
            }
        }

        public NormalizationEditorViewModel NormalizationEditorViewModel { get; }

        public NormalizationViewerViewModel NormalizationViewerViewModel { get; }

        public bool ViewerVisible
        {
            get => _viewerVisible;
            set
            {
                if (value.Equals(_viewerVisible)) return;
                _viewerVisible = value;
                OnPropertyChanged();
                if (value)
                {
                    NormalizationViewerViewModel.UpdateReplacementTreeSource().ContinueWith(task =>
                        EditorVisible = false);
                }
            }
        }

        public FrameworkElement Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new NormalizationSettingsUc(this);
                    ViewerVisible = true;
                }

                return _control;
            }
        }

        public bool IsOnUpdate
        {
            get => _isOnUpdate;
            private set
            {
                if (_isOnUpdate == value) return;
                _isOnUpdate = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<IAudioBook> TargetRecordsForEdit
        {
            get => NormalizationEditorViewModel.TargetRecords;
            set
            {
                NormalizationEditorViewModel.TargetRecords = value;
                EditorVisible = value.AnySafe();
                ViewerVisible = !value.AnySafe();
            }
        }

        public async Task RefreshData()
        {
            await NormalizationViewerViewModel.UpdateReplacementTreeSource();
        }

        public void FixComponentConfig()
        {
        }

        public void Dispose()
        {
            var control = _control;
            if (control != null)
            {
                _control = null;
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void RestoreLayout()
        {
        }

        public void StoreLayout()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler Disposed;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NormalizationEditorViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsOnEditMode":
                    ViewerVisible = !NormalizationEditorViewModel.IsOnEditMode;
                    EditorVisible = NormalizationEditorViewModel.IsOnEditMode;
                    break;
            }
        }
    }
}