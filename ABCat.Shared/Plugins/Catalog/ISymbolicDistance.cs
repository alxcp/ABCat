using System;
using System.Collections.Generic;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog
{
    public interface ISymbolicDistance : IExtComponent
    {
        void SetData(IReadOnlyCollection<Tuple<string, string>> datas);
        IReadOnlyCollection<ISymbolicDistanceResult> Search(string targetStr);
    }
}
