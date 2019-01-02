using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Sites
{
    /// <summary>
    ///     Плагин загрузки контента, связанного с записью каталога
    /// </summary>
    public interface IRecordTargetDownloaderPlugin : IExtComponent
    {
        /// <summary>
        ///     Метод асинхронной загрузки контента
        /// </summary>
        /// <param name="records">Список ID загружаемых записей</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task DownloadRecordTarget(HashSet<string> records, CancellationToken cancellationToken);
    }
}