using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.Shared.Plugins.Sites
{
    public interface IWebSiteParserPlugin : IExtComponent
    {
        int WebSiteId { get; }
        string DisplayName { get; }

        /// <summary>
        ///     Начать обновление списка записей
        /// </summary>
        /// <param name="recordGroupsIds"></param>
        /// <param name="cancellationToken"></param>
        Task DownloadRecordGroups(HashSet<string> recordGroupsIds, CancellationToken cancellationToken);

        Task DownloadRecords(IReadOnlyCollection<string> recordsIds, PageSources pageSource,
            CancellationToken cancellationToken);

        Task<string> DownloadRecordSourcePage(IAudioBook record, CancellationToken cancellationToken);

        HashSet<string> GetGroupKeys(bool forceRefresh);
        Uri GetRecordPageUrl([NotNull] IAudioBook record);
    }
}