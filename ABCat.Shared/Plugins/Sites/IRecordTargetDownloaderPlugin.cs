using System;
using System.Collections.Generic;
using System.Threading;
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
        /// <param name="smallProgressCallback">Метод сообщения о прогрессе выполнения загрузки одной записи</param>
        /// <param name="totalProgressCallback">Метод сообщения о прогрессе общего выполнения операции</param>
        /// <param name="completedCallback">Метод завершения операции</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        void BeginDownloadRecordTargetAsync(HashSet<string> records, Action<int, int, string> smallProgressCallback,
            Action<int, int, string> totalProgressCallback, Action<Exception> completedCallback,
            CancellationToken cancellationToken);
    }
}