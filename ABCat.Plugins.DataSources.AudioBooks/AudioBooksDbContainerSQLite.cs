using System;
using System.Collections.Generic;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using Component.Infrastructure.Factory;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [PerCallComponentInfo("1.0")]
    public class AudioBooksDbContainerSqLite : IDbContainer
    {
        private readonly Lazy<AudioBookSet> _audioBooks;
        private readonly Lazy<BinaryDataSet> _binaryDataSet;
        private readonly Lazy<ReplacementStringSet> _replacementStringSet;
        private readonly Lazy<UserDataSet> _userDataSet;

        private AudioBooksDbContainerSQLiteConfig _config;

        public AudioBooksDbContainerSqLite()
        {
            _audioBooks = new Lazy<AudioBookSet>(() =>
            {
                var needVacuum = _config.LastAudioBooksVacuum.AddDays(3) < DateTime.Now;
                var result = new AudioBookSet(_config.AudioBooksFilePath, AudioBooksDbContainerSQLiteConfig.LockContext,
                    needVacuum);
                if (needVacuum)
                {
                    lock (AudioBooksDbContainerSQLiteConfig.LockContext)
                    {
                        _config.LastAudioBooksVacuum = DateTime.Now;
                        _config.Save();
                    }
                }

                return result;
            }, true);
            _replacementStringSet =
                new Lazy<ReplacementStringSet>(
                    () =>
                        new ReplacementStringSet(_config.ProcessingSettingsFilePath,
                            AudioBooksDbContainerSQLiteConfig.LockContext), true);
            _userDataSet =
                new Lazy<UserDataSet>(
                    () => new UserDataSet(_config.UserDataFilePath, AudioBooksDbContainerSQLiteConfig.LockContext),
                    true);
            _binaryDataSet =
                new Lazy<BinaryDataSet>(
                    () => new BinaryDataSet(_config.BinaryDataFilePath, AudioBooksDbContainerSQLiteConfig.LockContext),
                    true);
        }

        public Config Config { get; set; }

        public IAudioBookGroupSet AudioBookGroupSet => (IAudioBookGroupSet) AudioBookSet;

        public IAudioBookSet AudioBookSet => _audioBooks.Value;

        public bool AutoSaveChanges { get; set; }

        public IBinaryDataSet BinaryDataSet => _binaryDataSet.Value;

        public IHiddenRecordSet HiddenRecordSet => (IHiddenRecordSet) UserDataSet;

        public IHiddenValueSet HiddenValueSet => (IHiddenValueSet) UserDataSet;


        public HashSet<string> RecordsCache { get; } = new HashSet<string>();

        public IReplacementStringSet ReplacementStringSet => _replacementStringSet.Value;

        public IUserDataSet UserDataSet => _userDataSet.Value;

        public Queue<string> WaitForParse { get; } = new Queue<string>();

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            _config = Config.Load<AudioBooksDbContainerSQLiteConfig>();
            var configCheckResult = _config.Check(correct);
            incorrectConfig = configCheckResult ? null : _config;
            return configCheckResult;
        }

        public void Dispose()
        {
            if (AutoSaveChanges) SaveChanges();
            Disposed.Fire(this);
        }

        public void SaveChanges()
        {
            AudioBookSet.SaveAudioBooks();
            AudioBookGroupSet.SaveAudioBookGroups();
            BinaryDataSet.SaveBinaryData();
        }

        public event EventHandler Disposed;
    }
}