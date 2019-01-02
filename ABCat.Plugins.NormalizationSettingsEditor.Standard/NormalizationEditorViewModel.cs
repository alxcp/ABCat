using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.ViewModels;

namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    public sealed class NormalizationEditorViewModel : ViewModelBase
    {
        // ReSharper disable once InconsistentNaming
        private static readonly IEnumerable<PropertyForSelect> _availableProperties = new[]
        {
            new PropertyForSelect {DisplayName = "Битрейт", FieldName = "bitrate"},
            new PropertyForSelect {DisplayName = "Издатель", FieldName = "publisher"},
            new PropertyForSelect {DisplayName = "Читает", FieldName = "reader"},
            new PropertyForSelect {DisplayName = "Автор", FieldName = "author"},
            new PropertyForSelect {DisplayName = "Жанр", FieldName = "genre"}
        };

        private string _currentPossibleValue;
        private bool _isOnEditMode;
        private bool _isOnWaitMode = true;

        private string _replacementValue;
        private PropertyForSelect _selectedProperty;
        private IEnumerable<IAudioBook> _targetRecords;

        public IEnumerable<PropertyForSelect> AvailableProperties => _availableProperties;

        public ICommand CancelCommand => CommandFactory.Get(() =>
        {
            ValuesForReplace.Clear();
            ReplacementValue = null;
            IsOnEditMode = false;
            OnPropertyChanged(nameof(IsOnEditMode));
        }, () => true);

        public ICommand CurrentAsReplacementValueCommand => CommandFactory.Get(
            () => { ReplacementValue = CurrentPossibleValue; },
            () => CurrentPossibleValue != null);

        public string CurrentPossibleValue
        {
            get => _currentPossibleValue;
            set
            {
                if (value == _currentPossibleValue) return;
                _currentPossibleValue = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public bool IsOnEditMode
        {
            get => _isOnEditMode;
            set
            {
                if (value.Equals(_isOnEditMode)) return;
                _isOnEditMode = value;
                IsOnWaitMode = !IsOnEditMode;
                OnPropertyChanged();
            }
        }

        public bool IsOnWaitMode
        {
            get => _isOnWaitMode;
            set
            {
                if (value.Equals(_isOnWaitMode)) return;
                _isOnWaitMode = value;
                IsOnEditMode = !IsOnWaitMode;
                OnPropertyChanged();
            }
        }

        public ICommand RemoveItemCommand => CommandFactory.Get(
            parameter => ValuesForReplace.Remove(parameter.ToString()),
            parameter => true);

        public string ReplacementValue
        {
            get => _replacementValue;
            set
            {
                if (value == _replacementValue) return;
                _replacementValue = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand => CommandFactory.Get(Save, IsCanSave);

        public PropertyForSelect SelectedProperty
        {
            get => _selectedProperty;
            set
            {
                if (value == _selectedProperty) return;
                _selectedProperty = value;
                UpdateValuesForReplace();
                OnPropertyChanged();
            }
        }

        public IEnumerable<IAudioBook> TargetRecords
        {
            get => _targetRecords;
            set
            {
                _targetRecords = value;
                if (SelectedProperty == null) SelectedProperty = AvailableProperties.First();
                UpdateValuesForReplace();
                IsOnEditMode = _targetRecords.AnySafe();
            }
        }

        public ObservableCollection<string> ValuesForReplace { get; } = new ObservableCollection<string>();

        private bool IsCanSave()
        {
            return !string.IsNullOrEmpty(ReplacementValue) &&
                   ValuesForReplace.Any(
                       item => string.Compare(item, ReplacementValue, StringComparison.OrdinalIgnoreCase) != 0);
        }

        private void Save()
        {
            using (var autoSave = Context.I.DbContainerAutoSave)
            {
                var dbContainer = autoSave.DBContainer;
                var existed =
                    dbContainer.ReplacementStringSet.GetReplacementStringsBy(SelectedProperty.FieldName)
                        .Where(
                            item =>
                                string.Compare(item.ReplaceValue, ReplacementValue,
                                    StringComparison.OrdinalIgnoreCase) !=
                                0)
                        .ToArray();

                var newReplacementStrings = new List<IReplacementString>();

                foreach (var valueForReplace in ValuesForReplace.Where(item => item != ReplacementValue))
                {
                    if (
                        existed.All(
                            item =>
                                string.Compare(item.PossibleValue, valueForReplace,
                                    StringComparison.OrdinalIgnoreCase) !=
                                0))
                    {
                        var replacementString = dbContainer.ReplacementStringSet.CreateReplacementString();
                        replacementString.PossibleValue = valueForReplace;
                        replacementString.RecordPropertyName = SelectedProperty.FieldName;
                        replacementString.ReplaceValue = ReplacementValue;
                        newReplacementStrings.Add(replacementString);
                    }
                }

                dbContainer.ReplacementStringSet.AddReplacementString(newReplacementStrings.ToArray());

                TargetRecords = new IAudioBook[0];
            }
        }

        private void UpdateValuesForReplace()
        {
            ValuesForReplace.Clear();

            foreach (var targetRecord in _targetRecords)
            {
                string value = null;
                switch (SelectedProperty.FieldName)
                {
                    case "bitrate":
                        value = targetRecord.Bitrate;
                        break;
                    case "publisher":
                        value = targetRecord.Publisher;
                        break;
                    case "reader":
                        value = targetRecord.Reader;
                        break;
                    case "author":
                        value = targetRecord.Author;
                        break;
                    case "genre":
                        value = targetRecord.Genre;
                        break;
                }

                if (!string.IsNullOrEmpty(value) &&
                    ValuesForReplace.All(item => string.Compare(item, value, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    ValuesForReplace.Add(value);
                }
            }
        }

        public class PropertyForSelect
        {
            public string DisplayName { get; set; }
            public string FieldName { get; set; }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}