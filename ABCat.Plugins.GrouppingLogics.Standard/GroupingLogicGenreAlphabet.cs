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
            using (var dbContainer = Context.I.CreateDbContainer(false))
            {
                var root = new Group(this) {Caption = "Все группы произведений", Level = 0};

                var records = dbContainer.AudioBookSet.GetRecordsAll().ToArray();
                var genres = GetGenres(records);

                var alphabetGroups = new Dictionary<char, Group>();

                foreach (var genre in genres)
                {
                    if (!alphabetGroups.TryGetValue(genre[0], out var alphabetGroup))
                    {
                        alphabetGroup = new Group(this)
                        {
                            Caption = genre[0].ToString(),
                            Level = 1,
                            Parent = root
                        };
                        alphabetGroups.Add(genre[0], alphabetGroup);
                        root.Children.Add(alphabetGroup);
                    }

                    var genreForDisplay = genre.Length <= 40 ? genre : genre.Substring(0, 37) + "...";
                    var recordsByGenre = records.Where(item =>
                            item.Genre.ToStringOrEmpty().IndexOf(genre, StringComparison.InvariantCultureIgnoreCase) >=
                            0)
                        .ToArray();

                    var genreGroup = new Group(this)
                    {
                        Parent = alphabetGroup,
                        Caption = $"{genreForDisplay} [{recordsByGenre.Length}]",
                        Level = 2
                    };

                    foreach (var audioBookKey in recordsByGenre.Select(item=>item.Key))
                    {
                        alphabetGroup.LinkedRecords.Add(audioBookKey);
                        genreGroup.LinkedRecords.Add(audioBookKey);
                    }

                    alphabetGroup.Children.Add(genreGroup);
                }

                foreach (var alphabetGroup in alphabetGroups)
                {
                    alphabetGroup.Value.Caption += $" [{alphabetGroup.Value.LinkedRecords.Count}]";
                }

                return root;
            }
        }

        private IReadOnlyCollection<string> GetGenres(IEnumerable<IAudioBook> records)
        {
            var allGenres = records.SelectMany(item => item.Genre.Split(',', '/')).Select(item => item.Trim())
                .GroupBy(item => item.ToLower())
                .ToDictionary(item => item.Key, item => item.Count());

            return allGenres.OrderByDescending(item => item.Value).Where(item => !item.Key.IsNullOrEmpty())
                .Select(item => item.Key.ChangeCase(Extensions.CaseTypes.FirstWord, true, false)).ToArray();
        }


        protected override IEnumerable<IAudioBook> GetRecordsInner(IDbContainer dbContainer, Group group,
            CancellationToken cancellationToken)
        {
            IEnumerable<IAudioBook> result;

            if (group == null || group.Level == 0)
            {
                result = dbContainer.AudioBookSet.GetRecordsAll().ToArray();
            }
            else
            {
                result = dbContainer.AudioBookSet.GetRecordsByKeys(group.LinkedRecords);
            }

            return result;
        }
    }
}