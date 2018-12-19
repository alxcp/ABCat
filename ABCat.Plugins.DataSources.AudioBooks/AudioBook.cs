using System;
using System.ComponentModel;
using System.Linq;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("AudioBook")]
    public class AudioBook : IAudioBook
    {
        private string _lastParsedLength;
        private TimeSpan _parsedLength;
        private int _parsedBitrate;

        [Column("Author")] public string Author { get; set; }

        [Ignore] public string AuthorNameForParse { get; set; }

        [Ignore] public string AuthorSurnameForParse { get; set; }

        [Column("Bitrate")] public string Bitrate { get; set; }

        [Column("Created")] public DateTime Created { get; set; }

        [Column("Description")] public string Description { get; set; }

        [Column("Genre")] public string Genre { get; set; }

        [Column("GroupKey")] public string GroupKey { get; set; }

        [Column("Key")] [Unique] public string Key { get; set; }

        [Column("LastUpdate")] public DateTime LastUpdate { get; set; }

        [Column("Length")] public string Length { get; set; }

        [Ignore]
        [DisplayName("Длительность (парсинг)")]
        public TimeSpan ParsedLength
        {
            get
            {
                if (_parsedLength == TimeSpan.MinValue || !Equals(Length, _lastParsedLength))
                {
                    _parsedLength = Length.ToTimeSpan();
                    _lastParsedLength = Length;
                }

                return _parsedLength;
            }
        }

        public int ParsedBitrate
        {
            get
            {
                if (_parsedBitrate == 0)
                {
                    TryParseBitrate(Bitrate, out _parsedBitrate);
                }

                return _parsedBitrate;
            }
        }

        private readonly static string[] _bitrateJunkStart = {"~", "vbr", "cbr"};

        private static readonly string[] _bitrateKbps =
            {"kbps", "kbit / sec", "kbit / s", "kb / s", "kbit", "кбит / сек", "кб / с", "к / с"};

        private bool TryParseBitrate(string bitrateString, out int bitrate)
        {
            bitrateString = bitrateString.ToLower();
            bitrate = 0;
            if (!bitrateString.IsNullOrEmpty())
            {
                string mainPart;
                var parts = bitrateString.Split(',', 'и');
                if (parts.Length > 1)
                    mainPart = parts
                        .FirstOrDefault(item => _bitrateKbps.Any(bs => item.IndexOf(bs, StringComparison.InvariantCultureIgnoreCase) >= 0));
                else
                    mainPart = bitrateString;

                if (!mainPart.IsNullOrEmpty())
                {
                    foreach (var s in _bitrateJunkStart)
                    {
                        mainPart = mainPart.Replace(s, "");
                    }

                    mainPart = mainPart.Trim();
                }
            }

            return false;
        }

        [Column("Size")] public long Size { get; set; }

        public TimeSpan ParsedLengthByBitrate { get; }

        [Column("Publisher")] public string Publisher { get; set; }

        [Column("Reader")] public string Reader { get; set; }

        [Column("MagnetLink")] public string MagnetLink { get; set; }

        [Column("Title")] public string Title { get; set; }

        [Column("OpenCounter")] public int OpenCounter { get; set; }

        public void ClearMetaInfo()
        {
            Author = null;
            Bitrate = null;
            Publisher = null;
            Size = 0;
            Reader = null;
            Description = null;
            Length = null;
            Genre = null;
        }

        public string GetPageKey()
        {
            //ToDo: Group.Key!!! Not ID
            return "TP_{0}_{1}".F(GroupKey, Key);
        }

        public string GetPageMetaKey()
        {
            //ToDo: Group.Key!!! Not ID
            return "TPM_{0}_{1}".F(GroupKey, Key);
        }

        public Uri GetRecordPageUrl()
        {
            return new Uri($"http://rutracker.org/forum/viewtopic.php?t={Key}");
        }

        public override string ToString()
        {
            return "[{0}] {1}".F(Key, Title);
        }
    }
}