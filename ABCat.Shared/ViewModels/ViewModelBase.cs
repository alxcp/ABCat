using System.ComponentModel;
using ABCat.Shared.Commands;
using JetBrains.Annotations;

namespace ABCat.Shared.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected CommandFactory CommandFactory { get; } = new CommandFactory();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}