using System;
using System.Collections.Generic;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBookSet : IObjectSet<IAudioBook>
    {
        void AddChangedRecords(params IAudioBook[] audioBooks);
        void AddRecord(IAudioBook record);
        IAudioBook CreateRecord();
        IReadOnlyCollection<IAudioBook> GetRecordsAllWithCache();
        IReadOnlyCollection<IAudioBook> GetRecordsAll();
        IEnumerable<IAudioBook> GetRecordsByGroup(string linkedObjectString);
        IReadOnlyCollection<IAudioBook> GetRecordsByKeys(HashSet<string> recordsKeys);
        IEnumerable<IAudioBook> GetRecordsUpdatedBefore(int webSiteId, DateTime lastUpdate);
        IEnumerable<IAudioBook> GetRecordsByWebSite(int webSiteId);

        void Import(string dbFilePath);
        void SaveAudioBooks();
    }
}