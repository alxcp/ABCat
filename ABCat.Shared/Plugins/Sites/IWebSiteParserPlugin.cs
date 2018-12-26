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

        int WebSiteId { get; }

        HashSet<string> GetGroupKeys(bool forceRefresh);
        Uri GetRecordPageUrl([NotNull] IAudioBook record);
        string DisplayName { get; }
    }
}