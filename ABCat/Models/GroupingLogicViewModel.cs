using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.Plugins.Catalog.GroupingLogics;
using ABCat.Shared.ViewModels;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public sealed class GroupingLogicViewModel : ViewModelBase
    {
        private readonly Action<Group> _selectedGroupChanged;

        private CancellationTokenSource _cancellationTokenSource;
        private IEnumerable<IGroupingLogicPlugin> _groupingPlugins;
        private bool _isOnUpdate;
        private IEnumerable<Group> _root;
        private Group _selectedGroup;
        private IGroupingLogicPlugin _selectedGroupingLogicPlugin;

        public GroupingLogicViewModel(IEnumerable<IGroupingLogicPlugin> groupingPlugins,
            Action<Group> selectedGroupChanged)
        {
            _selectedGroupChanged = selectedGroupChanged;
            GroupingPlugins = groupingPlugins;
            SelectedGroupingLogicPlugin = GroupingPlugins.FirstOrDefault();
        }

        public IEnumerable<IGroupingLogicPlugin> GroupingPlugins
        {
            get => _groupingPlugins;
            set
            {
                if (Equals(value, _groupingPlugins)) return;
                _groupingPlugins = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnUpdate
        {
            get => _isOnUpdate || (_selectedGroupingLogicPlugin?.IsOnUpdate).GetValueOrDefault();
            set
            {
                if (value.Equals(_isOnUpdate)) return;
                _isOnUpdate = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<Group> Root
        {
            get => _root;
            set
            {
                if (Equals(value, _root)) return;
                _root = value;
                OnPropertyChanged();
            }
        }

        public Group SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (Equals(value, _selectedGroup)) return;
                _selectedGroup = value;
                _selectedGroupChanged?.Invoke(value);
                OnPropertyChanged();
            }
        }

        public IGroupingLogicPlugin SelectedGroupingLogicPlugin
        {
            get => _selectedGroupingLogicPlugin;
            set
            {
                if (Equals(value, _selectedGroupingLogicPlugin)) return;
                if (_selectedGroupingLogicPlugin!= null)
                    _selectedGroupingLogicPlugin.PropertyChanged -= _selectedGroupingLogicPlugin_PropertyChanged;
                _selectedGroupingLogicPlugin = value;
                if (_selectedGroupingLogicPlugin != null)
                    _selectedGroupingLogicPlugin.PropertyChanged += _selectedGroupingLogicPlugin_PropertyChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOnUpdate));
                GenerateGroups(value);
            }
        }

        private void _selectedGroupingLogicPlugin_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsOnUpdate")
            {
                OnPropertyChanged(nameof(IsOnUpdate));
            }
        }

        public void Refresh()
        {
            GenerateGroups(SelectedGroupingLogicPlugin);
        }

        private async void GenerateGroups([NotNull] IGroupingLogicPlugin groupingLogic)
        {
            var cancellationTokenSource = _cancellationTokenSource;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationTokenSource;

            try
            {
                IsOnUpdate = true;

                var group = await groupingLogic.GenerateGroups(cancellationTokenSource.Token);

                Root = new[] {group};

                if (group != null)
                {
                    group.IsSelected = true;
                    group.IsExpanded = true;
                }

                SelectedGroup = group;
            }
            finally
            {
                IsOnUpdate = false;
            }
        }
    }
}