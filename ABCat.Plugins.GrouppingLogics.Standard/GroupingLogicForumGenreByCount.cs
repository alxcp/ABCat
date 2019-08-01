using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ABCat.Shared.Plugins.Catalog.GroupingLogics;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.GroupingLogics.Standard
{
    [SingletoneComponentInfo("1.0")]
    [UsedImplicitly]
    public class GroupingLogicForumGenreByCount : GroupingLogicPluginBase
    {
        public override string Name => "Форум → Жанр (↓Кол-во)";

        protected override Group GenerateGroupsInternal(CancellationToken cancellationToken)
        {
            var dbContainer = Context.I.DbContainer;
            var root = new Group(this) {Caption = RootGroupCaption, Level = 0};

            var recordGroups = dbContainer.AudioBookGroupSet.GetRecordGroupsAll()
                .ToDictionary(item => item.Key, item => item);
            if (cancellationToken.IsCancellationRequested) return null;
            var records =
                dbContainer.AudioBookSet.GetRecordsAllWithCache().GroupBy(record => record.GroupKey).ToArray();
            if (cancellationToken.IsCancellationRequested) return null;

            foreach (var grouping in records.OrderBy(item => item.Key == null ? "" : recordGroups[item.Key].Title))
            {
                if (cancellationToken.IsCancellationRequested) return null;
                var title = grouping.Key == null ? "" : recordGroups[grouping.Key].Title;

                var recordGroupGroup = new Group(this)
                {
                    LinkedObjectString = grouping.Key ?? "",
                    Level = 1,
                    Caption = $"{title} [{grouping.Count()}]"
                };

                root.Add(recordGroupGroup);

                var genreRecords = grouping
                    .GroupBy(record => record.Genre)
                    .OrderByDescending(item => item.Count())
                    .ThenBy(item => item.Key)
                    .ToArray();

                foreach (var genreRecord in genreRecords)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    var groupCaption = $"{genreRecord.Key} [{genreRecord.Count()}]";

                    var recordGenreGroup = new Group(this)
                    {
                        Caption = groupCaption,
                        Level = 2,
                        LinkedObjectString = genreRecord.Key
                    };

                    foreach (var audioBookKey in genreRecord.Select(item => item.Key))
                    {
                        recordGenreGroup.LinkedRecords.Add(audioBookKey);
                        recordGenreGroup.LinkedRecords.Add(audioBookKey);
                    }

                    recordGroupGroup.Add(recordGenreGroup);
                }
            }

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
            else if (group.Level == 1)
            {
                result = dbContainer.AudioBookSet.GetRecordsByGroup(group.LinkedObjectString).ToArray();
            }
            else
            {
                result = dbContainer.AudioBookSet.GetRecordsByKeys(group.LinkedRecords);
            }

            return result;
        }

        public override string ToString()
        {
            return "Groupping logic by Forum\\Genre (↓Count)";
        }
    }
}