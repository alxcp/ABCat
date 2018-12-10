using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Shared.Plugins.UI
{
    public interface IRecordsListPlugin : IControlPlugin
    {
        ContextMenu ContextMenu { get; set; }
        IEnumerable<IAudioBook> Data { get; set; }
        Dispatcher Dispatcher { get; }
        IEnumerable<IAudioBook> SelectedItems { get; }
        event EventHandler<ItemDoubleClickRowEventArgs> ItemDoubleClick;
    }
}