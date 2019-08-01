using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Component.Infrastructure
{
    public abstract class Saveable : INotifyPropertyChanged
    {
        private static readonly ConcurrentQueue<Saveable> ChangedItems = new ConcurrentQueue<Saveable>();
        private static readonly Timer ChangesTimer;

        private static DateTime _lastSave = DateTime.Now;

        private volatile int _updateCount;

        static Saveable()
        {
            var timerCallback = new TimerCallback(ChangesTimerElapsed);
            ChangesTimer = new Timer(timerCallback, null, 1000, Timeout.Infinite);
        }

        [Browsable(false)] public virtual bool CanSave => true;

        [Browsable(false)] public bool IsOnUpdate => _updateCount > 0;

        [Browsable(false)] public abstract string SaveGroupId { get; }

        public static bool AutoSaveEnabled { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void BeginUpdate()
        {
            _updateCount++;
        }

        public virtual void EndUpdate(bool force = false)
        {
            _updateCount = force || _updateCount <= 0 ? 0 : --_updateCount;
        }

        public abstract void Save();

        public virtual void SaveGroup(IEnumerable<Saveable> group)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (_updateCount == 0 && NeedSaveOnChangedProperty(propertyName)) AddItemToChanges(this);
        }

        protected virtual bool NeedSaveOnChangedProperty(string propertyName)
        {
            return true;
        }

        private static void AddItemToChanges(Saveable fileItemInfo)
        {
            if (!ChangedItems.Contains(fileItemInfo))
            {
                ChangedItems.Enqueue(fileItemInfo);
            }
        }

        public static bool SaveAll()
        {
            return SaveItems();
        }

        private static void ChangesTimerElapsed(object sender)
        {
            if (AutoSaveEnabled && (DateTime.Now - _lastSave).TotalSeconds > 5)
            {
                SaveItems();
            }

            ChangesTimer.Change(1000, Timeout.Infinite);
        }

        private static bool SaveItems()
        {
            var items = new List<Saveable>();
            var cannotSave = new List<Saveable>();
            while (ChangedItems.Count > 0)
            {
                if (ChangedItems.TryDequeue(out var saveable))
                {
                    if (saveable.CanSave) items.Add(saveable);
                    else cannotSave.Add(saveable);
                }
            }

            if (items.Any())
            {
                foreach (var saveable in items.GroupBy(item => item.SaveGroupId, item => item))
                {
                    if (saveable.Key == null || saveable.Count() == 1)
                    {
                        foreach (var saveable1 in saveable)
                        {
                            saveable1.Save();
                        }
                    }
                    else
                    {
                        saveable.First().SaveGroup(saveable);
                    }
                }

                foreach (var savable in cannotSave)
                {
                    AddItemToChanges(savable);
                }
            }

            _lastSave = DateTime.Now;

            return !ChangedItems.Any();
        }
    }
}