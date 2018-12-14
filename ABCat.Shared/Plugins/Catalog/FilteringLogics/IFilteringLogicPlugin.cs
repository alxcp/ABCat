using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog.FilteringLogics
{
    /// <summary>
    ///     Логика фильтрации записей каталога
    /// </summary>
    public interface IFilteringLogicPlugin : IExtComponent, INotifyPropertyChanged
    {
        /// <summary>
        ///     Условия фильтрации пусты
        /// </summary>
        [Browsable(false)]
        bool IsEmpty { get; }

        /// <summary>
        ///     Фильтр задействован
        /// </summary>
        [Category("Фильтр")]
        [DisplayName("Применить")]
        [Description("Применить фильтр")]
        bool IsEnabled { get; set; }

        /// <summary>
        ///     В данный момент выполняется асинхронная операция
        /// </summary>
        [Browsable(false)]
        bool IsOnUpdate { get; }

        /// <summary>
        ///     Выполнить фильтрацию записей каталога
        /// </summary>
        /// <param name="records">Записи</param>
        /// <param name="cancellationToken">Токен для отмены операции</param>
        Task<IEnumerable<IAudioBook>> Filter(IEnumerable<IAudioBook> records,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Обновить кеш значений фильтра
        /// </summary>
        Task UpdateCache(UpdateTypes updateType);

        /// <summary>
        ///     Выяснить подходит ли запись под условия фильтрации
        /// </summary>
        /// <param name="record">Запись</param>
        /// <returns>Подходит/не подходит</returns>
        bool FilterRecord(IAudioBook record);
    }
}