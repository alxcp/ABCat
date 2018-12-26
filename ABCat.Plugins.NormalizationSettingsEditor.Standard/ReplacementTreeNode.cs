using System.Collections.ObjectModel;
using System.Windows.Input;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.ViewModels;

namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    public class ReplacementTreeNode : ViewModelBase
    {
        private bool _isHidden;

        public ReplacementTreeNode(bool canHide)
        {
            CanHide = canHide;
        }

        public bool CanHide { get; }
        public bool CanRemove { get; set; }

        public ObservableCollection<ReplacementTreeNode> Children { get; set; }

        public ICommand HideCommand => CommandFactory.Get(async () =>
        {
            using (var autoSave = Context.I.DbContainerAutoSave)
            {
                var dbContainer = autoSave.DBContainer;
                var newHiddenValue = dbContainer.HiddenValueSet.CreateHiddenValue();
                newHiddenValue.WholeWord = true;
                newHiddenValue.IgnoreCase = true;
                newHiddenValue.PropertyName = RecordPropertyName;
                newHiddenValue.Value = ReplaceValue;
                dbContainer.HiddenValueSet.AddHiddenValue(newHiddenValue);
            }

            IsHidden = true;
            await Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>().UpdateCache(UpdateTypes.Hidden);
        }, () => !IsHidden);

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

        public ICommand UnHideCommand => CommandFactory.Get(async () =>
        {
            using (var dbContainer = Context.I.DbContainerAutoSave)
            {
                dbContainer.DBContainer.HiddenValueSet.Delete(RecordPropertyName, Value);
            }

            IsHidden = false;
            await Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>()
                .UpdateCache(UpdateTypes.Hidden);
        }, () => IsHidden);

        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}