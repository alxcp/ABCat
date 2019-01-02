using System;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog.ParsingLogics
{
    public interface INaturalTimeSpanParserPlugin : IExtComponent
    {
        TimeSpan Parse(string timeSpanString);
    }
}