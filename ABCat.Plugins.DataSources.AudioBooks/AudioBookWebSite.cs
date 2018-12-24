using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("WebSite")]
    public class AudioBookWebSite : IAudioBookWebSite
    {
        [Column("SiteParserPluginName")] public string SiteParserPluginName { get; set; }
        [PrimaryKey] [AutoIncrement] [Column("Id")] public int Id { get; set; }
    }
}