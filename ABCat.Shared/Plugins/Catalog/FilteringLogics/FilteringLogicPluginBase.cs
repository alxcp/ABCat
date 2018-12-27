using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.ViewModels;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog.FilteringLogics
{
    public abstract class FilteringLogicPluginBase : ViewModelBase, IFilteringLogicPlugin
    {
        private bool _enabled = true;

        private FilterFields _filterFields;
        private volatile bool _isOnUpdate;

        private static readonly string FilterFilePath = Path.Combine(Config.ConfigFolderPath, "Filter.bin");

        protected FilteringLogicPluginBase()
        {
            FilterFields = LoadFilter();
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

        [Browsable(false)]
        public ICommand ClearFilterCommand => CommandFactory.Get(() =>
        {
            FilterFields = new FilterFields();
        }, () => !IsEmpty);

        public virtual async Task<IEnumerable<IAudioBook>> Filter(IEnumerable<IAudioBook> records,
            CancellationToken cancellationToken)
        {
            IsOnUpdate = true;

            var asyncResult = await Task.Factory.StartNew(
                () =>
                {
                    try
                    {
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

            SaveFilter();

            return asyncResult;
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
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public abstract bool FilterRecord(IAudioBook record);

        public event EventHandler Disposed;

        protected abstract void UpdateCacheInternal(UpdateTypes updateType);

        private void SaveFilter()
        {
            using (var fs = new FileStream(FilterFilePath, FileMode.Create, FileAccess.Write))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(fs, FilterFields);
            }
        }

        private FilterFields LoadFilter()
        {
            FilterFields result = new FilterFields();

            if (File.Exists(FilterFilePath))
            {
                try
                {
                    using (var fs = new FileStream(FilterFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var bf = new BinaryFormatter();
                        result = (FilterFields) bf.Deserialize(fs);
                    }
                }
                catch (Exception ex)
                {
                    Context.I.MainLog.Error(ex);
                }
            }

            return result;
        }
    }
}