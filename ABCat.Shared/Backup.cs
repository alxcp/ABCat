using System;
using System.IO;

namespace ABCat.Shared
{
    public class FileBackupLogic : IDisposable
    {
        public readonly string BackupFileName;
        public readonly string FileName;
        public readonly string WriteFileName;

        public FileBackupLogic(string fileName)
        {
            FileName = fileName;
            WriteFileName = File.Exists(FileName) ? "{0}.tmp".F(FileName) : FileName;
            BackupFileName = "{0}.bak".F(FileName);
        }

        public void Dispose()
        {
            EndWrite();
        }

        public FileStream BeginWrite()
        {
            return new FileStream(WriteFileName, FileMode.Create, FileAccess.Write);
        }

        public void EndWrite()
        {
            if (WriteFileName != FileName && File.Exists(WriteFileName))
            {
                if (File.Exists(BackupFileName)) File.Delete(BackupFileName);
                File.Move(FileName, BackupFileName);
                File.Move(WriteFileName, FileName);
                File.Delete(BackupFileName);
            }
        }
    }
}