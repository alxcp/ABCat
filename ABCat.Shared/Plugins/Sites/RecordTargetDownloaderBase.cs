using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Sites
{
    public abstract class RecordTargetDownloaderBase : IRecordTargetDownloaderPlugin
    {
        public Config Config { get; set; }

        public async Task DownloadRecordTarget(HashSet<string> recordsIds,
            Action<int, int, string> smallProgressCallback, Action<int, int, string> totalProgressCallback,
            CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                var z = 0;
                using (var dbContainer = Context.I.CreateDbContainer(true))
                {
                    var records = dbContainer.AudioBookSet.GetRecordsByKeys(recordsIds).ToArray();

                    foreach (var record in records)
                    {
                        totalProgressCallback(z, records.Length, $"{z} / {record.Length}");
                        DownloadRecordTarget(null, record, dbContainer, smallProgressCallback,
                            cancellationToken);
                        if (cancellationToken.IsCancellationRequested) break;
                        dbContainer.SaveChanges();
                        z++;
                    }
                }
            }, cancellationToken);
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfig);

        public void Dispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Disposed;

        public abstract void DownloadRecordTarget(string loginCoockies, IAudioBook record, IDbContainer dbContainer,
            Action<int, int, string> progressCallback, CancellationToken cancellationToken);

        public abstract string GetAbsoluteLibraryPath(IAudioBook record);

        public abstract string GetRecordTargetLibraryPath(IAudioBook record);
    }
}