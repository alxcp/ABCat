using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("HiddenValue")]
    public class HiddenValue : IHiddenValue
    {
        [PrimaryKey] [AutoIncrement] public int Id { get; set; }

        public bool IgnoreCase { get; set; }

        [Indexed] public string PropertyName { get; set; }

        [Indexed] public string Value { get; set; }

        public bool WholeWord { get; set; }
    }
}