using System.ComponentModel;
using System.IO;
using ABCat.Core.Editors;
using Component.Infrastructure;
using JetBrains.Annotations;
using Shared.Everywhere;

namespace ABCat.Plugins.NormalizationLogic.Standard
{
    /// <summary>
    ///     Configuration for the literary (Flibusta-backed) stage of the normalization plugin.
    ///     The mechanical replacement-list stage needs no configuration; these settings only
    ///     control the reference-catalog matching and its overlay store.
    /// </summary>
    [UsedImplicitly]
    [DisplayName("Нормализация: сверка с каталогом")]
    public sealed class NormalizationLogicStandardConfig : Config
    {
        private bool _enableLiteraryMatching = true;
        private string _databaseFolder;

        [DisplayName("Сверять с эталонным каталогом")]
        [Description("Сопоставлять записи с каталогом Флибусты и сохранять референсы (автор, книга) в overlay-хранилище")]
        public bool EnableLiteraryMatching
        {
            get => _enableLiteraryMatching;
            set
            {
                if (value == _enableLiteraryMatching) return;
                _enableLiteraryMatching = value;
                OnPropertyChanged();
            }
        }

        [DisplayName("Папка БД")]
        [Description("Папка, где лежат reference-catalog.sqlite и создаётся Normalization.sqlite")]
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

        [Browsable(false)] public string ReferenceCatalogFileName => "reference-catalog.sqlite";

        [Browsable(false)] public string ReferenceCatalogPath => Path.Combine(DatabaseFolder ?? "", ReferenceCatalogFileName);

        [Browsable(false)] public string NormalizationDbFileName => "Normalization.sqlite";

        [Browsable(false)] public string NormalizationDbPath => Path.Combine(DatabaseFolder ?? "", NormalizationDbFileName);

        public override string DisplayName => "Нормализация: сверка с каталогом";

        public override bool CheckAndFix()
        {
            var result = true;
            if (string.IsNullOrEmpty(DatabaseFolder))
            {
                result = false;
                DatabaseFolder = SharedContext.I.GetAppDataFolderPath("DataBases");
            }

            Directory.CreateDirectory(DatabaseFolder);
            return result;
        }
    }
}
