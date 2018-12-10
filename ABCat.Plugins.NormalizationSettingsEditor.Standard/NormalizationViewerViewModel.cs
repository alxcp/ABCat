using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataSets;
using JetBrains.Annotations;

namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    public sealed class NormalizationViewerViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ReplacementTreeNode> _replacementTreeSource =
            new ObservableCollection<ReplacementTreeNode>();

        private bool _isEmpty;
        private bool _isOnUpdate = true;

        public NormalizationViewerViewModel()
        {
            BeginUpdateReplacementTreeSourceAsync();
            RemoveItemCommand = new DelegateCommand(parameter =>
            {
                var node = (ReplacementTreeNode) parameter;

                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
                    dbContainer.ReplacementStringSet.Delete(node.RecordPropertyName, node.ReplaceValue,
                        node.PossibleValue);
                }

                BeginUpdateReplacementTreeSourceAsync();
            }, parameter => true);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if (value.Equals(_isEmpty)) return;
                _isEmpty = value;
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

        public IEnumerable<ReplacementTreeNode> ReplacementTreeSource => _replacementTreeSource;

        public DelegateCommand RemoveItemCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void BeginUpdateReplacementTreeSourceAsync()
        {
            BeginBuildReplacementTreeAsync(SetSource);
        }

        private void SetSource(IEnumerable<ReplacementTreeNode> nodes)
        {
            var application = Application.Current;
            if (application != null)
            {
                if (application.Dispatcher.CheckAccess())
                {
                    _replacementTreeSource.Clear();
                    foreach (var replacementTreeNode in nodes)
                    {
                        _replacementTreeSource.Add(replacementTreeNode);
                    }

                    IsEmpty = !_replacementTreeSource.Any();
                }
                else
                {
                    application.Dispatcher.BeginInvoke((Action) (() => SetSource(nodes)));
                }
            }
        }

        private void BeginBuildReplacementTreeAsync(Action<IEnumerable<ReplacementTreeNode>> completedCallback)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    IsOnUpdate = true;
                    using (var dbContainer = Context.I.CreateDbContainer(false))
                    {
                        var hiddenRecords = dbContainer.HiddenValueSet.GetHiddenValuesAll().ToArray();

                        var result = new List<ReplacementTreeNode>();
                        var cache =
                            dbContainer.ReplacementStringSet.GetReplacementStringsAll()
                                .ToArray()
                                .GroupBy(item => item.RecordPropertyName);

                        foreach (var recordPropertyGroup in cache)
                        {
                            var groupNode = new ReplacementTreeNode(false);
                            groupNode.CanRemove = false;
                            var property =
                                typeof(IAudioBook).GetProperties()
                                    .FirstOrDefault(item => item.Name.ToLower() == recordPropertyGroup.Key);
                            if (property != null)
                            {
                                var displayNameAttribute =
                                    (DisplayNameAttribute)
                                    property.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                        .FirstOrDefault();
                                if (displayNameAttribute != null)
                                {
                                    groupNode.Value = displayNameAttribute.DisplayName;
                                }
                                else groupNode.Value = recordPropertyGroup.Key;
                            }
                            else groupNode.Value = recordPropertyGroup.Key;

                            groupNode.Children = new ObservableCollection<ReplacementTreeNode>();
                            result.Add(groupNode);

                            var sets = recordPropertyGroup.GroupBy(item => item.ReplaceValue,
                                item => item.PossibleValue);
                            foreach (var setValueGroup in sets)
                            {
                                var setNode = new ReplacementTreeNode(true);
                                var hiddenValue =
                                    hiddenRecords.FirstOrDefault(
                                        item =>
                                            item.PropertyName == recordPropertyGroup.Key &&
                                            item.Value == setValueGroup.Key);
                                setNode.IsHidden = hiddenValue != null;
                                setNode.RecordPropertyName = recordPropertyGroup.Key;
                                setNode.ReplaceValue = setValueGroup.Key;
                                setNode.CanRemove = true;
                                setNode.Value = setValueGroup.Key;
                                setNode.Children = new ObservableCollection<ReplacementTreeNode>();
                                groupNode.Children.Add(setNode);

                                foreach (var possibleValue in setValueGroup)
                                {
                                    var itemNode = new ReplacementTreeNode(false) {Value = possibleValue};
                                    itemNode.RecordPropertyName = recordPropertyGroup.Key;
                                    itemNode.ReplaceValue = setValueGroup.Key;
                                    itemNode.PossibleValue = possibleValue;
                                    itemNode.CanRemove = true;
                                    setNode.Children.Add(itemNode);
                                }
                            }
                        }

                        completedCallback(result);
                    }
                }
                finally
                {
                    IsOnUpdate = false;
                }
            });
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}