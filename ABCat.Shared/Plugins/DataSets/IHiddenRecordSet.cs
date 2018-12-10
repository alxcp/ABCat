using System.Collections.Generic;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IHiddenRecordSet : IObjectSet<IHiddenRecord>
    {
        void AddHiddenRecord(params IHiddenRecord[] hiddenRecord);
        IHiddenRecord CreateHiddenRecord();
        IEnumerable<IHiddenRecord> GetHiddenRecords(string groupName, HashSet<string> keys);
        IEnumerable<IHiddenRecord> GetHiddenRecordsAll();
    }
}