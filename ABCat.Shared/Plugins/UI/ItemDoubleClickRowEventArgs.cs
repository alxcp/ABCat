using System;
using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Shared.Plugins.UI
{
    public class ItemDoubleClickRowEventArgs : EventArgs
    {
        public readonly IAudioBook Target;

        public ItemDoubleClickRowEventArgs(IAudioBook target)
        {
            Target = target;
        }
    }
}