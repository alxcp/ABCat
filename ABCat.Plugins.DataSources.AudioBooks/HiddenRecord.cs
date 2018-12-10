using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("HiddenRecord")]
    public class HiddenRecord : IHiddenRecord
    {
        [Indexed] public string RecordGroupKey { get; set; }

        [Indexed] public string RecordKey { get; set; }
    }
}