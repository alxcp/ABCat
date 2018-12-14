using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.ViewModels;

namespace ABCat.Plugins.NormalizationSettingsEditor.Standard
{
    public sealed class NormalizationViewerViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ReplacementTreeNode> _replacementTreeSource =
            new ObservableCollection<ReplacementTreeNode>();

        private bool _isEmpty;
        private bool _isOnUpdate = true;

        public NormalizationViewerViewModel()
        {
            UpdateReplacementTreeSource().ContinueWith(task => { });
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

        public ICommand RemoveItemCommand => CommandFactory.Get(async parameter =>
        {
            var node = (ReplacementTreeNode) parameter;

            using (var dbContainer = Context.I.CreateDbContainer(true))
            {
                dbContainer.ReplacementStringSet.Delete(node.RecordPropertyName, node.ReplaceValue,
                    node.PossibleValue);
            }

            await UpdateReplacementTreeSource();
        }, parameter => true);

        public async Task UpdateReplacementTreeSource()
        {
            SetSource(await BuildReplacementTree());
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

        private async Task<IReadOnlyCollection<ReplacementTreeNode>> BuildReplacementTree()
        {
            return await Task.Factory.StartNew(() =>
            {
                IsOnUpdate = true;

                try
                {
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
                            var groupNode = new ReplacementTreeNode(false) {CanRemove = false};
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
                                    var itemNode = new ReplacementTreeNode(false)
                                    {
                                        Value = possibleValue,
                                        RecordPropertyName = recordPropertyGroup.Key,
                                        ReplaceValue = setValueGroup.Key,
                                        PossibleValue = possibleValue,
                                        CanRemove = true
                                    };
                                    setNode.Children.Add(itemNode);
                                }
                            }
                        }

                        return result;
                    }
                }
                finally
                {
                    IsOnUpdate = false;
                }
            });
        }
    }
}