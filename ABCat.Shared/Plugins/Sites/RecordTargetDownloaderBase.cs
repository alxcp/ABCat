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

        public void BeginDownloadRecordTargetAsync(HashSet<string> recordsIds,
            Action<int, int, string> smallProgressCallback, Action<int, int, string> totalProgressCallback,
            Action<Exception> completedCallback, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var z = 0;
                    using (var dbContainer = Context.I.CreateDbContainer(true))
                    {
                        var records = dbContainer.AudioBookSet.GetRecordsByKeys(recordsIds).ToArray();

                        foreach (var record in records)
                        {
                            totalProgressCallback(z, records.Count(), "{0} из {1}".F(z, records.Count()));
                            DownloadRecordTarget(null, record, dbContainer, smallProgressCallback,
                                cancellationToken);
                            if (cancellationToken.IsCancellationRequested) break;
                            dbContainer.SaveChanges();
                            z++;
                        }
                    }

                    completedCallback(null);
                }
                catch (Exception ex)
                {
                    completedCallback(ex);
                }
            }, cancellationToken);
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfig);

        public void Dispose()
        {
            Disposed.Fire(this);
        }

        public event EventHandler Disposed;

        public abstract void DownloadRecordTarget(string loginCoockies, IAudioBook record, IDbContainer dbContainer,
            Action<int, int, string> progressCallback, CancellationToken cancellationToken);

        public abstract string GetAbsoluteLibraryPath(IAudioBook record);

        public abstract string GetRecordTargetLibraryPath(IAudioBook record);
    }
}