using System.Collections.Generic;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.DataProviders
{
    public interface IDbContainer : IExtComponent
    {
        IAudioBookGroupSet AudioBookGroupSet { get; }
        IAudioBookSet AudioBookSet { get; }
        bool AutoSaveChanges { get; set; }
        IBinaryDataSet BinaryDataSet { get; }
        IHiddenRecordSet HiddenRecordSet { get; }
        IHiddenValueSet HiddenValueSet { get; }
        HashSet<string> RecordsCache { get; }
        IReplacementStringSet ReplacementStringSet { get; }
        IUserDataSet UserDataSet { get; }
        Queue<string> WaitForParse { get; }

        void SaveChanges();
    }
}