using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Shared.Plugins.Catalog.GroupingLogics
{
    public class WebSiteGroup : Group
    {
        public WebSiteGroup(GroupingLogicPluginBase ownerLogic, IAudioBookWebSite webSite) : base(ownerLogic)
        {
            WebSite = webSite;
        }

        public IAudioBookWebSite WebSite { get; }
    }
}