using System;
using ABCat.Shared.Plugins.Catalog.GroupingLogics;

namespace ABCat.UI.WPF.UI
{
    public class GroupChangedEventArgs : EventArgs
    {
        public readonly Group Group;

        public GroupChangedEventArgs(Group group)
        {
            Group = group;
        }
    }
}