using System;
using System.ComponentModel;
using System.Text;
using ABCat.Shared.Plugins.DataProviders;
using Component.Infrastructure.Factory;

namespace ABCat.Shared
{
    public interface IContext : INotifyPropertyChanged
    {
        ILog Logger { get; }
        IComponentFactory ComponentFactory { get; }
        IEventAggregatorShared EventAggregator { get; }
        Encoding DefaultEncoding { get; }
        //IDbContainer CreateDbContainer(bool autoSave);
        IDbContainer DbContainer { get; }
        DbContainerAutoSave DbContainerAutoSave { get; }
    }
}