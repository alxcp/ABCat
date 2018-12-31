using System;

namespace ABCat.Shared.Plugins.Catalog.FilteringLogics
{
    [Flags]
    public enum UpdateTypes
    {
        Values = 1,
        Hidden = 2,
        Loaded = 4,
        All = Values | Hidden | Loaded
    }
}