using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("WebSite")]
    public class AudioBookWebSite : IAudioBookWebSite
    {
        [Column("WebSiteParserPluginName")] public string WebSiteParserPluginName { get; set; }

        [PrimaryKey]
        [AutoIncrement]
        [Column("Id")]
        public int Id { get; set; }

        [Column("DisplayName")] public string DisplayName { get; set; }
    }
}