using System.ComponentModel;
using System.Text;

namespace Shared.Everywhere
{
    public interface ISharedContext : INotifyPropertyChanged
    {
        IEventAggregatorShared EventAggregator { get; }
        Encoding DefaultEncoding { get; }
        string AppDataFolderPath { get; }
        string GetAppDataFolderPath(string subfolderName);
    }
}