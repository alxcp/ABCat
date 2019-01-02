using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog.ParsingLogics
{
    public interface INaturalBitrateParserPlugin : IExtComponent
    {
        bool TryParseBitrate(string bitrateString, out int bitrate);
    }
}