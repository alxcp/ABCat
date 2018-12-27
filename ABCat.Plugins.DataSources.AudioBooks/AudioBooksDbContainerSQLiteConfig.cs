using System;
using System.ComponentModel;
using System.IO;
using ABCat.Core.Editors;
using ABCat.Shared;
using ABCat.Shared.Messages;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public delegate void ExecuteWithLock(Action action);

    [UsedImplicitly]
    [DisplayName("База данных аудиокниг")]
    public sealed class AudioBooksDbContainerSQLiteConfig : Config
    {
        private static readonly object ExecuteWithLockObj = new object();

        public static void ExecuteWithLockDelegate(Action action)
        {
            lock (ExecuteWithLockObj)
            {
                Context.I.EventAggregator.PublishOnUIThread(new DBOperationMessage(DBOperationMessage.OperationStates.Started));
                action();
                Context.I.EventAggregator.PublishOnUIThread(new DBOperationMessage(DBOperationMessage.OperationStates.Finished));
            }
        }

        private string _databaseFolder;

        [Browsable(false)] public string AudioBooksFileName => "AudioBooks.sqlite";

        [Browsable(false)] public string AudioBooksFilePath => Path.Combine(DatabaseFolder, AudioBooksFileName);

        [Browsable(false)] public string BinaryDataFileName => "BinaryData.sqlite";

        [Browsable(false)] public string BinaryDataFilePath => Path.Combine(DatabaseFolder, BinaryDataFileName);

        [DisplayName("Папка БД аудиокниг")]
        [Description("Папка для хранения БД аудиокниг")]
        [Editor(typeof(FolderPathEditor), typeof(FolderPathEditor))]
        public string DatabaseFolder
        {
            get => _databaseFolder;
            set
            {
                if (Equals(value, _databaseFolder)) return;
                _databaseFolder = value;
                OnPropertyChanged();
            }
        }

        public override string DisplayName => "База данных аудиокниг";

        [Browsable(false)] public DateTime LastAudioBooksVacuum { get; set; }

        [Browsable(false)] public string ProcessingSettingsFileName => "ProcessingSettings.sqlite";

        [Browsable(false)]
        public string ProcessingSettingsFilePath => Path.Combine(DatabaseFolder, ProcessingSettingsFileName);

        [Browsable(false)] public string UserDataFileName => "UserData.sqlite";

        [Browsable(false)] public string UserDataFilePath => Path.Combine(DatabaseFolder, UserDataFileName);

        public override bool Check(bool correct)
        {
            var result = true;

            ExecuteWithLockDelegate(() =>
            {
                if (Extensions.IsNullOrEmpty(DatabaseFolder))
                {
                    result = false;
                    DatabaseFolder = SharedContext.I.GetAppDataFolderPath("DataBases");
                }

                if (LastAudioBooksVacuum == DateTime.MinValue) LastAudioBooksVacuum = DateTime.Now;

                if (correct)
                {
                    Directory.CreateDirectory(DatabaseFolder);
                    if (!File.Exists(AudioBooksFilePath) &&
                        File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB", AudioBooksFileName)))
                    {
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB", AudioBooksFileName),
                            AudioBooksFilePath);
                    }

                    if (!File.Exists(BinaryDataFilePath) &&
                        File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB", BinaryDataFileName)))
                    {
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DB", BinaryDataFileName),
                            BinaryDataFilePath);
                    }
                }
            });

            return result;
        }
    }
}