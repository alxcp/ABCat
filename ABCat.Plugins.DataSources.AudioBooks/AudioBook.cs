using System;
using System.ComponentModel;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("AudioBook")]
    public class AudioBook : IAudioBook
    {
        private string _lastParsedLenght;
        private TimeSpan _parsedLenght;

        //[Column("Author")] public string Author { get; set; }

        [Ignore] public string AuthorNameForParse { get; set; }

        [Ignore] public string AuthorSurnameForParse { get; set; }

        [Column("Bitrate")] public string Bitrate { get; set; }

        [Column("Created")] public DateTime Created { get; set; }

        [Column("Description")] public string Description { get; set; }

        [Column("Genre")] public string Genre { get; set; }

        [Column("GroupKey")] public string GroupKey { get; set; }

        [Column("Key")] [Unique] public string Key { get; set; }

        [Column("LastUpdate")] public DateTime LastUpdate { get; set; }

        [Column("Lenght")] public string Lenght { get; set; }

        [Ignore]
        [DisplayName("Длительность (парсинг)")]
        public TimeSpan ParsedLenght
        {
            get
            {
                if (_parsedLenght == TimeSpan.MinValue || !Equals(Lenght, _lastParsedLenght))
                {
                    _parsedLenght = Lenght.ToTimeSpan();
                    _lastParsedLenght = Lenght;
                }

                return _parsedLenght;
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
            Reader = null;
            Description = null;
            Lenght = null;
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