using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ABCat.Shared;
using ABCat.Shared.Plugins.Catalog.GroupingLogics;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.GroupingLogics.Standard
{
    [SingletoneComponentInfo("1.0")]
    [UsedImplicitly]
    public class GroupingLogicGenreAlphabet : GroupingLogicPluginBase
    {
        public override string Name => "Жанр";

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        protected override Group GenerateGroupsInternal(CancellationToken cancellationToken)
        {
            var dbContainer = Context.I.DbContainer;
            var root = new Group(this) {Caption = "Все группы произведений", Level = 0};

            var records = dbContainer.AudioBookSet.GetRecordsAllWithCache();
            var recordsByGenre = new Dictionary<string, Group>(StringComparer.InvariantCultureIgnoreCase);
            var alphabetGroups = new Dictionary<string, Group>(StringComparer.InvariantCultureIgnoreCase);

            var emptyGenreGroup = new Group(this)
            {
                Caption = "<Не задано>",
                Level = 1,
            };
            root.Add(emptyGenreGroup);

            foreach (var audioBook in records)
            {
                var genres = audioBook.GetGenres();
                if (genres.Any())
                {
                    foreach (var genre in genres)
                    {
                        var firstLetter = genre[0].ToString().ToUpper();

                        if (!alphabetGroups.TryGetValue(firstLetter, out var alphabetGroup))
                        {
                            alphabetGroup = new Group(this)
                            {
                                Caption = firstLetter,
                                Level = 1,
                            };
                            alphabetGroups.Add(firstLetter.ToUpper(), alphabetGroup);
                            root.Add(alphabetGroup);
                        }

                        if (!recordsByGenre.TryGetValue(genre, out var group))
                        {
                            var genreForDisplay =
                                (genre.Length <= 40 ? genre : genre.Substring(0, 39) + "…").ChangeCase(
                                    Extensions.CaseTypes.AllWords, false, false);

                            group = new Group(this)
                            {
                                Caption = genreForDisplay,
                                Level = 2
                            };

                            recordsByGenre[genre] = group;
                            alphabetGroup.Add(group);
                        }

                        group.LinkedRecords.Add(audioBook.Key);
                        alphabetGroup.LinkedRecords.Add(audioBook.Key);
                    }
                }
                else
                {
                    emptyGenreGroup.LinkedRecords.Add(audioBook.Key);
                }
            }

            foreach (var genreGroup in recordsByGenre)
            {
                genreGroup.Value.Caption = $"{genreGroup.Value.Caption} [{genreGroup.Value.LinkedRecords.Count}]";
            }

            foreach (var alphabetGroup in alphabetGroups)
            {
                alphabetGroup.Value.Caption += $" [{alphabetGroup.Value.LinkedRecords.Count}]";
                alphabetGroup.Value.OrderByLinkedRecordsQuantity();
            }

            root.OrderByCaption();

            return root;
        }

        protected override IEnumerable<IAudioBook> GetRecordsInner(IDbContainer dbContainer, Group group,
            CancellationToken cancellationToken)
        {
            IEnumerable<IAudioBook> result;

            if (group == null || group.Level == 0)
            {
                result = dbContainer.AudioBookSet.GetRecordsAllWithCache();
            }
            else
            {
                result = dbContainer.AudioBookSet.GetRecordsByKeys(group.LinkedRecords);
            }

            return result;
        }
    }
}