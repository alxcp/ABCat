using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ABCat.Core.Editors;
using ABCat.Shared;
using ABCat.Shared.Plugins.Downloaders;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.Plugins.Parsers.Rutracker
{
    [UsedImplicitly]
    [DisplayName("Загрузка торрент-файлов")]
    public sealed class RutrackerTorrentFilesDownloaderConfig : Config
    {
        public enum TorrentFileActionEnum
        {
            [Description("Запустить загрузку в торрент-клиенте")]
            StartDownload,
            [Description("Показать в проводнике")] None,

            [Description("Выполнить системное действие")]
            SystemAction
        }

        private readonly IEnumerable<ITorrentFileDownloaderPlugin> _torrentFileDownloaders;
        private string _audioCatalogFolder;
        private string _torrentClientName;
        private TorrentFileActionEnum _torrentFileAction;
        private string _torrentFilesFolder;

        public RutrackerTorrentFilesDownloaderConfig()
        {
            _torrentFileDownloaders = Context.I.ComponentFactory.GetCreators<ITorrentFileDownloaderPlugin>()
                .Select(item => item.GetInstance<ITorrentFileDownloaderPlugin>()).Where(item => item.IsExists);

            ComboBoxEditor.Register("TorrentClientName",
                new ObservableCollection<object>(_torrentFileDownloaders.Select(item => item.DisplayName)));
        }

        [DisplayName("Каталог контента")]
        [Description("Путь к папке, в которой будет храниться загружаемый контент." +
                     "ABCat может создавать иерархию внутри этой папки согласно логике группировки контента. " +
                     "У ABCat должны быть права на запись в указанную папку.")]
        [Editor(typeof(FolderPathEditor), typeof(FolderPathEditor))]
        public string AudioCatalogFolder
        {
            get => _audioCatalogFolder;
            set
            {
                if (value == _audioCatalogFolder) return;
                _audioCatalogFolder = value;
                OnPropertyChanged();
            }
        }

        public override string DisplayName => "Загрузка торрент-файлов";

        [DisplayName("Torrent-клиент")]
        [Description("Найденные установки Torrent-клиентов, поддерживаемые ABCat")]
        [Editor(typeof(ComboBoxEditor), typeof(ComboBoxEditor))]
        public string TorrentClientName
        {
            get => _torrentClientName;
            set
            {
                if (value == _torrentClientName) return;
                _torrentClientName = value;
                OnPropertyChanged();
            }
        }

        [DisplayName("Действие")]
        [Description("Что нужно делать с загруженными torrent-файлами")]
        [Editor(typeof(EnumEditor), typeof(EnumEditor))]
        public TorrentFileActionEnum TorrentFileAction
        {
            get => _torrentFileAction;
            set
            {
                if (value == _torrentFileAction) return;
                _torrentFileAction = value;
                OnPropertyChanged();
            }
        }

        [DisplayName("Хранилище torrent-файлов")]
        [Description("Папка для сохранения загруженных *.torrent файлов")]
        [Editor(typeof(FolderPathEditor), typeof(FolderPathEditor))]
        public string TorrentFilesFolder
        {
            get => _torrentFilesFolder;
            set
            {
                if (value == _torrentFilesFolder) return;
                _torrentFilesFolder = value;
                OnPropertyChanged();
            }
        }

        public override bool Check(bool correct)
        {
            var result = true;

            if (Extensions.IsNullOrEmpty(AudioCatalogFolder))
                AudioCatalogFolder = SharedContext.I.GetAppDataFolderPath("AudioCatalog");
            if (Extensions.IsNullOrEmpty(TorrentFilesFolder))
                TorrentFilesFolder = SharedContext.I.GetAppDataFolderPath("TorrentFiles");

            var downloaders = Context.I.ComponentFactory.GetCreators<ITorrentFileDownloaderPlugin>()
                .Select(item => item.GetInstance<ITorrentFileDownloaderPlugin>()).ToArray();

            if (TorrentClientName.IsNullOrEmpty())
            {
                if (downloaders.AnySafe())
                {
                    var firstClient = downloaders.First();
                    TorrentClientName = firstClient.DisplayName;
                }
            }
            else
            {
                TorrentClientName = TorrentClientName.Split('[').First().Trim();
            }

            if (!Directory.Exists(AudioCatalogFolder))
            {
                result = false;
                if (correct) Directory.CreateDirectory(AudioCatalogFolder);
            }

            if (!Directory.Exists(TorrentFilesFolder))
            {
                result = false;
                if (correct) Directory.CreateDirectory(TorrentFilesFolder);
            }

            result = result && CheckDirectory(AudioCatalogFolder) && CheckDirectory(TorrentFilesFolder);

            if (TorrentFileAction == TorrentFileActionEnum.StartDownload)
            {
                if (Extensions.IsNullOrEmpty(TorrentClientName)) result = false;
                else
                {
                    var selected = downloaders.FirstOrDefault(item => item.DisplayName == TorrentClientName);

                    result = result && selected != null && selected.IsExists;
                }
            }

            return result;
        }
    }
}