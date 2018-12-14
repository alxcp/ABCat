﻿using System.Collections.ObjectModel;
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

        public ICommand HideCommand => CommandFactory.Get(() =>
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

        public ICommand UnHideCommand => CommandFactory.Get(() =>
        {
            using (var dbContainer = Context.I.CreateDbContainer(true))
            {
                dbContainer.HiddenValueSet.Delete(RecordPropertyName, Value);
            }

            IsHidden = false;
            Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>()
                .BeginUpdateCacheAsync(UpdateTypes.Hidden, ex => { });
        }, () => IsHidden);

        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}