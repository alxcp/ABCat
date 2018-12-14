using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ABCat.Shared.Plugins.Catalog.GrouppingLogics;
using ABCat.Shared.ViewModels;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public sealed class GrouppingLogicViewModel : ViewModelBase
    {
        private readonly Action<Group> _selectedGroupChanged;

        private CancellationTokenSource _cancellationTokenSource;
        private IEnumerable<IGrouppingLogicPlugin> _grouppingPlugins;
        private bool _isOnUpdate;
        private IEnumerable<Group> _root;
        private Group _selectedGroup;
        private IGrouppingLogicPlugin _selectedGrouppingLogicPlugin;

        public GrouppingLogicViewModel(IEnumerable<IGrouppingLogicPlugin> grouppingPlugins,
            Action<Group> selectedGroupChanged)
        {
            _selectedGroupChanged = selectedGroupChanged;
            GrouppingPlugins = grouppingPlugins;
            SelectedGrouppingLogicPlugin = GrouppingPlugins.FirstOrDefault();
        }

        public IEnumerable<IGrouppingLogicPlugin> GrouppingPlugins
        {
            get => _grouppingPlugins;
            set
            {
                if (Equals(value, _grouppingPlugins)) return;
                _grouppingPlugins = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnUpdate
        {
            get => _isOnUpdate;
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

        public IGrouppingLogicPlugin SelectedGrouppingLogicPlugin
        {
            get => _selectedGrouppingLogicPlugin;
            set
            {
                if (Equals(value, _selectedGrouppingLogicPlugin)) return;
                _selectedGrouppingLogicPlugin = value;
                OnPropertyChanged();
                BeginGenerateGroupsAsync(value);
            }
        }

        public void Refresh()
        {
            BeginGenerateGroupsAsync(SelectedGrouppingLogicPlugin);
        }

        private async void BeginGenerateGroupsAsync([NotNull] IGrouppingLogicPlugin grouppingLogic)
        {
            var cancellationTokenSource = _cancellationTokenSource;
            if (cancellationTokenSource != null) cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationTokenSource;

            try
            {
                IsOnUpdate = true;

                var group = await grouppingLogic.BeginGenerateGroupsAsync(cancellationTokenSource.Token);

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