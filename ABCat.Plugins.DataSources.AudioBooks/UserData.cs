using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("UserData")]
    public class UserData : IUserData
    {
        public string LocalPath { get; set; }

        [Indexed] public string RecordGroupKey { get; set; }

        [Indexed] public string RecordKey { get; set; }
    }
}