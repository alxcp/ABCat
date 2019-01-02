using ABCat.Shared.ViewModels;

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