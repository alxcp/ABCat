using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("ReplacementString")]
    public class ReplacementString : IReplacementString
    {
        [PrimaryKey] [AutoIncrement] public int Id { get; set; }

        [Indexed] public string PossibleValue { get; set; }

        [Indexed] public string RecordPropertyName { get; set; }

        [Indexed] public string ReplaceValue { get; set; }
    }
}