using System.ComponentModel;
using System.Runtime.CompilerServices;
using ABCat.Shared.ViewModels;
using JetBrains.Annotations;

namespace ABCat.Shared.Plugins.UI
{
    public class ShowProgressViewModel : ViewModelBase
    {
        private string _title;

        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }
    }
}