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

namespace ABCat.Shared.Plugins.Catalog.GrouppingLogics
{
    /// <summary>
    ///     Базовый класс логики группировки записей
    /// </summary>
    public abstract class GrouppingLogicPluginBase : IGrouppingLogicPlugin
    {
        private bool _isOnUpdate;

        public Config Config { get; set; }

        /// <summary>
        ///     Значение свойства изменено
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     В данный момент выполняется асинхронная операция
        /// </summary>
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

        /// <summary>
        ///     Название логики группировки
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     Начать асинхронную генерацию дерева групп
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
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

        /// <summary>
        ///     Начать асинхронное получение записей, включенных в группу
        /// </summary>
        /// <param name="dbContainer">Контейнер БД</param>
        /// <param name="group">Группа записей</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        public async Task<IEnumerable<IAudioBook>> GetRecords(
            IDbContainer dbContainer,
            Group group,
            CancellationToken cancellationToken)
        {
            try
            {
                IsOnUpdate = true;

                return
                    await
                        Task.Factory.StartNew(
                            () => GetRecordsInner(dbContainer, group, cancellationToken),
                            cancellationToken);
            }
            finally
            {
                IsOnUpdate = false;
            }
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfig);

        /// <summary>
        ///     Уничтожение экземпляра класса логики группировки
        /// </summary>
        public void Dispose()
        {
            Disposed.Fire(this);
        }

        /// <summary>
        ///     Логика группировки уничтожена
        /// </summary>
        public event EventHandler Disposed;

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Логика формирования дерева групп
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Корневая группа сформированного дерева</returns>
        protected abstract Group GenerateGroupsInternal(CancellationToken cancellationToken);

        /// <summary>
        ///     Логика получения списка записей, включенных в группу
        /// </summary>
        /// <param name="dbContainer">Контейнер БД</param>
        /// <param name="group">Группа записей</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Список записей, включенных в группу</returns>
        protected abstract IEnumerable<IAudioBook> GetRecordsInner(IDbContainer dbContainer, Group group,
            CancellationToken cancellationToken);

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}