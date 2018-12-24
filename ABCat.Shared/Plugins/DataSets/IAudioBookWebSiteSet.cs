using System.Collections.Generic;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBookWebSiteSet : IObjectSet<IAudioBookWebSite>
    {
        void AddWebSite(IAudioBookWebSite webSite);
        IAudioBookWebSite CreateWebSite();
        IEnumerable<IAudioBookWebSite> GetWebSitesAll();
    }
}