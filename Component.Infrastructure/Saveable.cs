using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Component.Infrastructure
{
    /// <summary>
    ///     Базовый класс для классов, поддерживающих автосохранение при редактировании
    /// </summary>
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

        /// <summary>
        ///     Можно сохранять в данный момент
        /// </summary>
        [Browsable(false)]
        public virtual bool CanSave => true;

        /// <summary>
        ///     Класс в данный момент находится в процессе инициализации
        /// </summary>
        [Browsable(false)]
        public bool IsOnUpdate => _updateCount > 0;

        /// <summary>
        ///     Код группы сохраняемых объектов.
        ///     При SaveGroupId != null все объекты с одинаковым значением кода сохраняются групповым методом сохранения SaveGroup.
        ///     Иначе каждый объект сохраняется индивидуально методом Save.
        /// </summary>
        [Browsable(false)]
        public abstract string SaveGroupId { get; }

        public static bool AutoSaveEnabled { get; set; }

        /// <summary>
        ///     Событие изменения свойства класса
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Начать инициализацию класса.
        ///     Класс перестаёт реагировать на изменение свойств и не запускает таймер сохранения.
        ///     Ведётся счетчик количества начала инициализаций.
        /// </summary>
        public virtual void BeginUpdate()
        {
            _updateCount++;
        }

        /// <summary>
        ///     Закончить инициализацию класса.
        ///     Класс начинается реагировать на изменение свойств.
        ///     Ведётся счетчик количества начала инициализаций.
        /// </summary>
        /// <param name="force">Обнулить счётчик количества начала инициализаций (IsOnUpdate сразу становится True)</param>
        public virtual void EndUpdate(bool force = false)
        {
            _updateCount = force || _updateCount <= 0 ? 0 : --_updateCount;
        }

        /// <summary>
        ///     Сохранить объект
        /// </summary>
        public abstract void Save();

        /// <summary>
        ///     Сохранить группу объектов
        /// </summary>
        /// <param name="group">Список объектов с одинаковым SaveGroupId</param>
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
                Saveable saveable;
                if (ChangedItems.TryDequeue(out saveable))
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