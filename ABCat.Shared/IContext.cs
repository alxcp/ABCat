using ABCat.Shared.Plugins.DataProviders;
using Component.Infrastructure.Factory;
using Shared.Everywhere;

namespace ABCat.Shared
{
    public interface IContext : ISharedContext
    {
        ILog MainLog { get; }
        IComponentFactory ComponentFactory { get; }
        IDbContainer DbContainer { get; }
        DbContainerAutoSave DbContainerAutoSave { get; }
    }
}