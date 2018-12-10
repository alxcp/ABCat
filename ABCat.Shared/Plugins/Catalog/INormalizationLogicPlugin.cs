using System.Collections.Generic;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog
{
    /// <summary>
    ///     Интерфейс плагина нормализации записей
    /// </summary>
    public interface INormalizationLogicPlugin : IExtComponent
    {
        /// <summary>
        ///     Метод выполнения нормализации группы записей
        /// </summary>
        /// <param name="records">Группа записей</param>
        /// <param name="dbContainer">Контейнер БД</param>
        void Normalize(IEnumerable<IAudioBook> records, IDbContainer dbContainer);
    }
}