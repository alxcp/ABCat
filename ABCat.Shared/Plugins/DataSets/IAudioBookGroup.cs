using System;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBookGroup
    {
        string Key { get; set; }
        int LastPageCount { get; set; }
        DateTime LastUpdate { get; set; }
        string Title { get; set; }
    }
}