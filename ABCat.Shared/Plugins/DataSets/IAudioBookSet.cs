using System;
using System.Collections.Generic;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBookSet : IObjectSet<IAudioBook>
    {
        void AddChangedRecords(params IAudioBook[] audioBooks);
        void AddRecord(IAudioBook record);
        IAudioBook CreateRecord();
        IEnumerable<IAudioBook> GetRecordsAll();
        IEnumerable<IAudioBook> GetRecordsByGroup(string linkedObjectString);
        IEnumerable<IAudioBook> GetRecordsByKeys(HashSet<string> recordsKeys);

        IEnumerable<IAudioBook> GetRecordsUpdatedBefore(DateTime lastUpdate);

        void Import(string dbFilePath);
        void SaveAudioBooks();
    }
}