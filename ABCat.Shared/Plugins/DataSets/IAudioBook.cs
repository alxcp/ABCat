using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IAudioBook : IRecord
    {
        [DisplayName("Автор")] string Author { get; set; }

        [Browsable(false)] string AuthorNameForParse { get; set; }

        [Browsable(false)] string AuthorSurnameForParse { get; set; }

        [DisplayName("Битрейт")] string Bitrate { get; set; }

        [DisplayName("Жанр")] string Genre { get; set; }

        [DisplayName("Длина")] string Length { get; set; }

        [DisplayName("Длина (Парсинг)")] TimeSpan ParsedLength { get; }

        [UsedImplicitly] [DisplayName("Битрейт (Парсинг)")] int ParsedBitrate { get; }

        [DisplayName("Размер, Байт")] long Size { get; set; }

        [UsedImplicitly] [DisplayName("Длина (По битрейту)")] TimeSpan ParsedLengthByBitrate { get; }

        [DisplayName("Издатель")] string Publisher { get; set; }

        [DisplayName("Читает")] string Reader { get; set; }

        [Browsable(false)] string MagnetLink { get; set; }

        [DisplayName("Просмотрено")] int OpenCounter { get; set; }

        [Browsable(false)]
        IReadOnlyCollection<string> GetGenres();

        [Browsable(false)]
        IReadOnlyCollection<string> GetAuthors();
    }
}