using ABCat.Shared.Plugins.DataProviders;
using Component.Infrastructure.Factory;
using Shared.Everywhere;

namespace ABCat.Shared
{
    public interface IContext : ISharedContext
    {
        ILog Logger { get; }
        IComponentFactory ComponentFactory { get; }
        IDbContainer DbContainer { get; }
        DbContainerAutoSave DbContainerAutoSave { get; }
    }
}