using System.Windows.Input;
using ABCat.UI.WPF.Models;

namespace ABCat.UI.WPF.UI
{
    public partial class GroupsTreeUc
    {
        public GroupsTreeUc()
        {
            InitializeComponent();
        }

        public GrouppingLogicViewModel ViewModel
        {
            get => (GrouppingLogicViewModel) DataContext;
            set => DataContext = value;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.F5:
                    var viewModel = ViewModel;
                    viewModel?.Refresh();

                    break;
            }
        }
    }
}