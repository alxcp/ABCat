using System;
using System.Collections.Generic;
using System.Threading;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Sites
{
    public interface ISiteParserPlugin : IExtComponent
    {
        /// <summary>
        ///     Начать обновление списка записей
        /// </summary>
        /// <param name="recordGroupsIds"></param>
        /// <param name="smallProgressCallback"></param>
        /// <param name="totalProgressCallback"></param>
        /// <param name="completedCallback"></param>
        /// <param name="cancellationToken"></param>
        void BeginDownloadRecordGroupsAsync(HashSet<string> recordGroupsIds,
            Action<int, int, string> smallProgressCallback, Action<int, int, string> totalProgressCallback,
            Action<Exception> completedCallback, CancellationToken cancellationToken);

        void BeginDownloadRecordsAsync(HashSet<string> recordsIds, PageSources pageSource,
            Action<int, int, string> smallProgressCallback, Action<int, int, string> totalProgressCallback,
            Action<Exception> completedCallback, CancellationToken cancellationToken);

        void BeginDownloadRecordSourcePageAsync(IAudioBook record, Action<string, Exception> completedCallback,
            CancellationToken cancellationToken);
    }
}