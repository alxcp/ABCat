using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Sites
{
    //public delegate void ProgressCallback(int current, int total, string message);

    public interface ISiteParserPlugin : IExtComponent
    {
        /// <summary>
        ///     Начать обновление списка записей
        /// </summary>
        /// <param name="recordGroupsIds"></param>
        /// <param name="cancellationToken"></param>
        Task DownloadRecordGroups(HashSet<string> recordGroupsIds, CancellationToken cancellationToken);

        Task DownloadRecords(HashSet<string> recordsIds, PageSources pageSource,
            CancellationToken cancellationToken);

        Task OrganizeKeywords(CancellationToken cancellationToken);

        Task<string> DownloadRecordSourcePage(IAudioBook record, CancellationToken cancellationToken);
    }
}