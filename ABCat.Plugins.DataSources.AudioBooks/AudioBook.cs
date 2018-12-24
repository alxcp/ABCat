using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using ABCat.Shared;
using ABCat.Shared.Plugins.Catalog.ParsingLogics;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("AudioBook")]
    public class AudioBook : IAudioBook
    {
        private static readonly INaturalTimeSpanParserPlugin TimeSpanParser;
        private static readonly INaturalBitrateParserPlugin BitrateParser;
        private string _lastParsedLength;
        private TimeSpan _parsedLength;
        private int _parsedBitrate;

        static AudioBook()
        {
            TimeSpanParser = Context.I.ComponentFactory.CreateActual<INaturalTimeSpanParserPlugin>();
            BitrateParser = Context.I.ComponentFactory.CreateActual<INaturalBitrateParserPlugin>();
        }

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
                    _parsedLength = TimeSpanParser.Parse(Length);
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
                    BitrateParser.TryParseBitrate(Bitrate, out _parsedBitrate);
                }

                return _parsedBitrate;
            }
        }

        [Column("Size")] public long Size { get; set; }

        private TimeSpan _parsedLengthByBitrate;

        public TimeSpan ParsedLengthByBitrate
        {
            get
            {
                if (_parsedLengthByBitrate == default(TimeSpan))
                {
                    if (Size > 0)
                    {
                        var pBitrate = ParsedBitrate;
                        var parsedLength = ParsedLength;

                        if (pBitrate > 0)
                        {
                            var seconds = Size / (pBitrate * 1024 / 8);
                            _parsedLengthByBitrate = TimeSpan.FromSeconds(seconds);
                            if (parsedLength != TimeSpan.Zero)
                            {
                                var k = parsedLength.TotalMilliseconds / _parsedLengthByBitrate.TotalMilliseconds;
                                if (k > 0.8 && k < 1.2)
                                {
                                    _parsedLengthByBitrate = parsedLength;
                                }
                            }
                        }
                        else
                        {
                            _parsedLengthByBitrate = parsedLength;
                        }
                    }
                }

                return _parsedLengthByBitrate;
            }
        }

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

        public override string ToString()
        {
            return "[{0}] {1}".F(Key, Title);
        }
    }
}