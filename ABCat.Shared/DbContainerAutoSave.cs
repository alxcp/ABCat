using System;
using ABCat.Shared.Plugins.DataProviders;

namespace ABCat.Shared
{
    public class DbContainerAutoSave : IDisposable
    {
        public DbContainerAutoSave(IDbContainer dbContainer)
        {
            DBContainer = dbContainer;
        }

        public IDbContainer DBContainer { get; }

        public void Dispose()
        {
            DBContainer.SaveChanges();
        }
    }
}