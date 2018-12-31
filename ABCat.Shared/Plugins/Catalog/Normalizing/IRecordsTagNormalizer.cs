using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog.Normalizing
{
    public interface IRecordsTagNormalizer : IExtComponent
    {
        Task Normalize(IReadOnlyCollection<string> recordsKeys, CancellationToken cancellationToken);
    }
}
