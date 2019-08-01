using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.Shared.Plugins.Catalog.GroupingLogics
{
    public abstract class GroupingLogicPluginBase : IGroupingLogicPlugin
    {
        protected static string RootGroupCaption => "Все группы произведений";
        protected static string ValueNotSetCaption => "<Не задано>";
        protected static string OtherValuesCaption => "<Другое…>";

        private bool _isOnUpdate;

        public Config Config { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public abstract string Name { get; }

        public async Task<Group> GenerateGroups(CancellationToken cancellationToken)
        {
            IsOnUpdate = true;
            return await Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        var result = GenerateGroupsInternal(cancellationToken);
                        return result;
                    }
                    finally
                    {
                        IsOnUpdate = false;
                    }
                }, cancellationToken);
        }

        public virtual void FixComponentConfig()
        {
        }

        public void Dispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Disposed;

        public override string ToString()
        {
            return Name;
        }

        protected abstract Group GenerateGroupsInternal(CancellationToken cancellationToken);

        protected abstract IEnumerable<IAudioBook> GetRecordsInner(IDbContainer dbContainer, Group group,
            CancellationToken cancellationToken);

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}