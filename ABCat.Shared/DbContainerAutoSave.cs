using System;
using ABCat.Shared.Plugins.DataProviders;

namespace ABCat.Shared
{
    public class DbContainerAutoSave : IDisposable
    {
        private readonly IDbContainer _dbContainer;

        public DbContainerAutoSave(IDbContainer dbContainer)
        {
            _dbContainer = dbContainer;
        }

        public IDbContainer DBContainer => _dbContainer;

        public void Dispose()
        {
            _dbContainer.SaveChanges();
        }
    }
}