using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.Catalog.Normalizing;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.NormalizationLogic.Standard
{
    [SingletoneComponentInfo("2.2")]
    [UsedImplicitly]
    public class AudioBookAuthorNormalizer : IRecordsTagNormalizer
    {
        private static readonly TimeSpan SaveTimer = TimeSpan.FromSeconds(5);
        private readonly Regex _mixedCyrLatRegex = new Regex("([A-Za-z]+[А-Яа-я]+)");

        public async Task Normalize(IReadOnlyCollection<string> recordKeys, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;
                    var records = dbContainer.AudioBookSet.GetRecordsAllWithCache();

                    var sw = new Stopwatch();
                    sw.Start();

                    var abQuantity = 0;

                    foreach (var audioBook in records)
                    {
                        var audioBookAuthors = audioBook.GetAuthors();
                        var authorsNormalized = new List<string>();

                        foreach (var audioBookAuthor in audioBookAuthors.Where(item => item.Length > 3))
                        {
                            var author4Resolve = _mixedCyrLatRegex.Replace(audioBookAuthor, CyrLatToCyrConverter);
                            if (author4Resolve != audioBookAuthor)
                            {
                                authorsNormalized.Add(author4Resolve);
                            }
                        }

                        if (authorsNormalized.Any())
                        {
                            audioBook.Author = string.Join("/ ", authorsNormalized.OrderBy(item => item));
                            dbContainer.AudioBookSet.AddChangedRecords(audioBook);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        ProgressMessage.ReportComplex(abQuantity++, records.Count);

                        if (sw.Elapsed > SaveTimer)
                        {
                            dbContainer.SaveChanges();
                            sw.Restart();
                        }
                    }

                    ProgressMessage.ReportComplex(records.Count, records.Count);
                }
            }, cancellationToken);
        }

        public void Dispose()
        {
        }

        public virtual void FixComponentConfig()
        {
        }

        private static string CyrLatToCyrConverter(Match m)
        {
            return new string(m.Value.Select(Resolve).ToArray());
        }

        private static char Resolve(char item)
        {
            switch (item)
            {
                case 'A':
                    return 'А';
                case 'a':
                    return 'а';
                case 'K':
                    return 'К';
                case 'k':
                    return 'к';
                case 'C':
                    return 'С';
                case 'c':
                    return 'с';
                case 'T':
                    return 'Т';
                case 't':
                    return 'т';
                case 'O':
                    return 'О';
                case 'o':
                    return 'о';
                case 'P':
                    return 'Р';
                case 'p':
                    return 'р';
                case 'H':
                    return 'Н';
                case 'h':
                    return 'н';
                case 'X':
                    return 'Х';
                case 'x':
                    return 'х';
                case 'M':
                    return 'М';
                case 'm':
                    return 'м';
                case 'E':
                    return 'Е';
                case 'e':
                    return 'е';
                case 'B':
                    return 'В';
                case 'G':
                    return 'Г';
                case 'g':
                    return 'г';
                case 'Z':
                    return 'Z';
                case 'z':
                    return 'з';
                default:
                    return item;
            }
        }
    }
}