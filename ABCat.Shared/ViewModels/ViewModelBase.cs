using System.ComponentModel;
using System.Runtime.CompilerServices;
using ABCat.Shared.Commands;
using JetBrains.Annotations;

namespace ABCat.Shared.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected CommandFactory CommandFactory { get; } = new CommandFactory();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            CommandFactory.UpdateAll();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}