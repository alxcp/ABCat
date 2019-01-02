using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Sites
{
    public abstract class RecordTargetDownloaderBase : IRecordTargetDownloaderPlugin
    {
        public Config Config { get; set; }

        public async Task DownloadRecordTarget(HashSet<string> recordsIds, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                var z = 0;
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;

                    var records = dbContainer.AudioBookSet.GetRecordsByKeys(recordsIds).ToArray();

                    foreach (var record in records)
                    {
                        ProgressMessage.Report(z, records.Length);
                        DownloadRecordTarget(null, record, dbContainer, cancellationToken);
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
            CancellationToken cancellationToken);

        public abstract string GetAbsoluteLibraryPath(IAudioBook record);

        public abstract string GetRecordTargetLibraryPath(IAudioBook record);
    }
}