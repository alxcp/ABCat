using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.Catalog.GroupingLogics;

namespace ABCat.Shared.Messages
{
    public class SelectedGroupChangedMessage
    {
        public Group SelectedGroup { get; set; }

        public SelectedGroupChangedMessage(Group selectedGroup)
        {
            SelectedGroup = selectedGroup;
        }
    }
}
