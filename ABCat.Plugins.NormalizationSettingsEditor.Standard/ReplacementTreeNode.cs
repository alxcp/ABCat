using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ABCat.Shared;
using ABCat.Shared.Commands;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using JetBrains.Annotations;

namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    public class ReplacementTreeNode : INotifyPropertyChanged
    {
        private readonly DelegateCommand _hideCommand;
        private readonly DelegateCommand _unHideCommand;

        private bool _isHidden;

        public ReplacementTreeNode(bool canHide)
        {
            CanHide = canHide;
            _hideCommand = new DelegateCommand(parameter =>
            {
                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
                    var newHiddenValue = dbContainer.HiddenValueSet.CreateHiddenValue();
                    newHiddenValue.WholeWord = true;
                    newHiddenValue.IgnoreCase = true;
                    newHiddenValue.PropertyName = RecordPropertyName;
                    newHiddenValue.Value = ReplaceValue;
                    dbContainer.HiddenValueSet.AddHiddenValue(newHiddenValue);
                }

                IsHidden = true;
                Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>()
                    .BeginUpdateCacheAsync(UpdateTypes.Hidden, ex => { });
            }, parameter => !IsHidden);

            _unHideCommand = new DelegateCommand(parameter =>
            {
                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
                    dbContainer.HiddenValueSet.Delete(RecordPropertyName, Value);
                }

                IsHidden = false;
                Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>()
                    .BeginUpdateCacheAsync(UpdateTypes.Hidden, ex => { });
            }, parameter => IsHidden);
        }

        public bool CanHide { get; }
        public bool CanRemove { get; set; }

        public ObservableCollection<ReplacementTreeNode> Children { get; set; }

        public ICommand HideCommand => _hideCommand;

        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                if (_isHidden == value) return;
                _isHidden = value;
                OnPropertyChanged();
            }
        }

        public string PossibleValue { get; set; }
        public string RecordPropertyName { get; set; }
        public string ReplaceValue { get; set; }

        public ICommand UnHideCommand => _unHideCommand;

        public string Value { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Value;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}