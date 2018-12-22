using System;
using System.Collections.Generic;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog
{
    public interface ISymbolicDistance : IExtComponent
    {
        void SetData(List<Tuple<string, string>> datas);
        List<Tuple<string, string, double, int>> Search(string targetStr);
    }
}
