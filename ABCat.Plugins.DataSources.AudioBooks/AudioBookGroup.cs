using System;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("AudioBookGroup")]
    public class AudioBookGroup : IAudioBookGroup
    {
        [PrimaryKey] [AutoIncrement] public int Id { get; set; }

        [Column("Key")] [Unique] public string Key { get; set; }

        [Ignore] public int LastPageCount { get; set; }

        [Column("LastUpdate")] public DateTime LastUpdate { get; set; }

        [Column("Title")] public string Title { get; set; }

        [Column("WebSiteId")] public int WebSiteId { get; set; }
    }
}