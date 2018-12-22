using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ABCat.Shared;
using ABCat.Shared.Plugins.Catalog;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using Component.Infrastructure.Factory;

namespace ABCat.Plugins.NormalizationLogic.Standard
{
    [SingletoneComponentInfo("1.0")]
    public class NormalizationLogicStandardPlugin : INormalizationLogicPlugin
    {
        private readonly ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();

        public NormalizationLogicStandardPlugin()
        {
            using (var dbContainer = Context.I.CreateDbContainer(false))
            {
                var allBooks = dbContainer.AudioBookSet.GetRecordsAll();
                foreach (var audioBook in allBooks)
                {
                    _authors.TryAdd(audioBook.Author.ToLower(), audioBook.Author);
                }
            }
        }

        public Config Config { get; set; }

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        public void Dispose()
        {
            Disposed.Fire(this);
        }

        public void Normalize(IReadOnlyCollection<IAudioBook> records, IDbContainer dbContainer)
        {
            var recordArray = records as IAudioBook[] ?? records.ToArray();
            var cache = dbContainer.ReplacementStringSet.GetReplacementStringsAll().ToArray();

            foreach (var record in recordArray)
            {
                var bitrateReplacement =
                    cache.FirstOrDefault(
                        str =>
                            str.RecordPropertyName == "bitrate" &&
                            string.Compare(str.PossibleValue, record.Bitrate, StringComparison.OrdinalIgnoreCase) == 0);
                if (bitrateReplacement != null) record.Bitrate = bitrateReplacement.ReplaceValue;

                var publisherReplacement =
                    cache.FirstOrDefault(
                        str =>
                            str.RecordPropertyName == "publisher" &&
                            string.Compare(str.PossibleValue, record.Publisher, StringComparison.OrdinalIgnoreCase) ==
                            0);
                if (publisherReplacement != null) record.Publisher = publisherReplacement.ReplaceValue;

                var readerReplacement =
                    cache.FirstOrDefault(
                        str =>
                            str.RecordPropertyName == "reader" &&
                            string.Compare(str.PossibleValue, record.Reader, StringComparison.OrdinalIgnoreCase) == 0);
                if (readerReplacement != null) record.Reader = readerReplacement.ReplaceValue;

                var authorName = CalcAuthorName(record);
                var authorReplacement =
                    cache.FirstOrDefault(
                        str =>
                            str.RecordPropertyName == "author" &&
                            string.Compare(str.PossibleValue, authorName, StringComparison.OrdinalIgnoreCase) == 0);

                record.Author = authorReplacement != null ? authorReplacement.ReplaceValue : authorName;

                if (record.Author.IsNullOrEmpty())
                {
                    if (!record.Title.IsNullOrEmpty())
                    {
                        var parts = record.Title.Split(" - ");
                        if (parts.Length > 1)
                        {
                            var possibleAuthor = parts[0].Trim().ToLower();
                            if (_authors.TryGetValue(possibleAuthor, out var author)) record.Author = author;
                        }
                    }
                }
                else _authors.TryAdd(authorName, authorName);

                var genreName = record.Genre ?? string.Empty;
                var genreReplacement =
                    cache.FirstOrDefault(
                        str =>
                            str.RecordPropertyName == "genre" &&
                            string.Compare(str.PossibleValue, genreName, StringComparison.OrdinalIgnoreCase) == 0);
                if (genreReplacement != null) record.Genre = genreReplacement.ReplaceValue;
                else record.Genre = genreName;
            }
        }

        public event EventHandler Disposed;

        public virtual string CalcAuthorName(IAudioBook record)
        {
            var result = record.Author;

            if (string.IsNullOrEmpty(result))
            {
                if (!string.IsNullOrEmpty(record.AuthorNameForParse) &&
                    !string.IsNullOrEmpty(record.AuthorSurnameForParse))
                {
                    result = "{0}, {1}".F(record.AuthorSurnameForParse, record.AuthorNameForParse);
                }
                else if (string.IsNullOrEmpty(record.AuthorNameForParse) &&
                         string.IsNullOrEmpty(record.AuthorSurnameForParse))
                {
                    result = string.Empty;
                }
                else
                {
                    result = record.AuthorSurnameForParse + record.AuthorNameForParse;
                }
            }

            return result;
        }
    }
}