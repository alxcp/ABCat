using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Properties;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.Shared.Plugins.Catalog.FilteringLogics
{
    public abstract class FilteringLogicPluginBase : IFilteringLogicPlugin
    {
        private bool _enabled = true;

        private FilterFields _filterFields;
        private volatile bool _isOnUpdate;

        protected FilteringLogicPluginBase()
        {
            FilterFields = new FilterFields();
        }

        [Browsable(false)] public Config Config { get; set; }

        [Browsable(false)]
        public FilterFields FilterFields
        {
            get => _filterFields;
            set
            {
                _filterFields = value;
                foreach (var propertyInfo in GetType().GetProperties())
                {
                    OnPropertyChanged(propertyInfo.Name);
                }
            }
        }

        [Browsable(false)]
        public ObservableCollection<FilterFields> FiltersList { get; } = new ObservableCollection<FilterFields>();

        public event PropertyChangedEventHandler PropertyChanged;

        [Browsable(false)] public abstract bool IsEmpty { get; }

        [Category("Фильтр")]
        [DisplayName("Применить")]
        [Description("Применить фильтр")]
        public bool IsEnabled
        {
            get => _enabled;
            set
            {
                if (value.Equals(_enabled)) return;
                _enabled = value;
                OnPropertyChanged();
            }
        }

        [Browsable(false)]
        public virtual bool IsOnUpdate
        {
            get => _isOnUpdate;
            set
            {
                if (value.Equals(_isOnUpdate)) return;
                _isOnUpdate = value;
                OnPropertyChanged();
            }
        }

        public virtual async Task<IEnumerable<IAudioBook>> Filter(IEnumerable<IAudioBook> records,
            CancellationToken cancellationToken)
        {
            return await Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        IsOnUpdate = true;
                        Thread.Sleep(500);
                        cancellationToken.ThrowIfCancellationRequested();

                        IAudioBook[] result;
                        try
                        {
                            IsOnUpdate = true;
                            result =
                                records.TakeWhile(record => !cancellationToken.IsCancellationRequested)
                                    .Where(FilterRecord)
                                    .ToArray();
                        }
                        finally
                        {
                            IsOnUpdate = false;
                        }

                        return result;
                    }
                    finally
                    {
                        IsOnUpdate = false;
                    }
                }, cancellationToken);
        }

        public virtual async Task UpdateCache(UpdateTypes updateType)
        {
            await Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        IsOnUpdate = true;
                        UpdateCacheInternal(updateType);
                    }
                    finally
                    {
                        IsOnUpdate = false;
                    }
                }
            );
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfig);

        public virtual void Dispose()
        {
            Disposed.Fire(this, EventArgs.Empty);
        }

        public abstract bool FilterRecord(IAudioBook record);

        public event EventHandler Disposed;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected abstract void UpdateCacheInternal(UpdateTypes updateType);
    }
}