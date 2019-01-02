namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBookWebSite
    {
        string WebSiteParserPluginName { get; set; }
        int Id { get; }
        string DisplayName { get; set; }
    }
}