using System;
using System.Collections.Generic;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBookGroupSet : IObjectSet<IAudioBookGroup>
    {
        void AddChangedRecordGroups(params IAudioBookGroup[] audioBookGroups);
        void AddRecordGroup(IAudioBookGroup group);
        IAudioBookGroup CreateRecordGroup();
        IAudioBookGroup GetRecordGroupByKey(string key);
        IEnumerable<IAudioBookGroup> GetRecordGroupsAll();
        IEnumerable<IAudioBookGroup> GetRecordGroupsByIds(HashSet<int> groupIds);
        IEnumerable<IAudioBookGroup> GetRecordGroupsUpdatedBefore(DateTime lastUpdate);
        void SaveAudioBookGroups();
    }
}