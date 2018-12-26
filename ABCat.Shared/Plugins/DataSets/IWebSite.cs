using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBookWebSite
    {
        string WebSiteParserPluginName { get; set; }
        int Id { get; }
        string DisplayName { get; set; }
    }
}
