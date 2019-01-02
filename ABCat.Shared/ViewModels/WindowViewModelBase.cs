using System;
using System.Windows.Input;

namespace ABCat.Shared.ViewModels
{
    public abstract class WindowViewModelBase : ViewModelBase
    {
        public virtual string WindowTitle => "Window Title";

        public ICommand CloseWindowCommand
        {
            get
            {
                return CommandFactory.Get(() => { CloseWindow?.Invoke(this, EventArgs.Empty); }, "CloseWindowCommand");
            }
        }

        public ICommand CollapseWindowsCommand
        {
            get
            {
                return CommandFactory.Get(() => { CollapseWindow?.Invoke(this, EventArgs.Empty); },
                    "CollapseWindowCommand");
            }
        }

        //public virtual Icon Icon
        //{
        //    get { return Resources.ApplicationIcon; }
        //}

        //public virtual string CloseWindowToolTip
        //{
        //    get { return Resources.WindowCloseButtonToolTip; }
        //}

        public abstract bool AllowCollapse { get; }
        public event EventHandler CloseWindow;
        public event EventHandler CollapseWindow;
    }
}