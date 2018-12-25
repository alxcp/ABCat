using System;
using System.ComponentModel;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IRecord
    {
        [DisplayName("Загружено")] DateTime Created { get; set; }

        [Browsable(false)] string Description { get; set; }

        [Browsable(false)] string GroupKey { get; set; }

        [Browsable(false)] string Key { get; set; }

        [DisplayName("Обновлено")] DateTime LastUpdate { get; set; }

        [DisplayName("Название")] string Title { get; set; }

        void ClearMetaInfo();
        string GetPageKey();
    }
}