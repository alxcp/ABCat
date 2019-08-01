using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class GroupingLogicForumAuthorByCount : GroupingLogicPluginBase
    {
        public override string Name => "Форум → Автор (↓Кол-во)";

        protected override Group GenerateGroupsInternal(CancellationToken cancellationToken)
        {
            var dbContainer = Context.I.DbContainer;
            var root = new Group(this) {Caption = RootGroupCaption, Level = 0};

            var recordGroups = dbContainer.AudioBookGroupSet.GetRecordGroupsAll()
                .ToDictionary(item => item.Key, item => item);
            if (cancellationToken.IsCancellationRequested) return null;
            var records =
                dbContainer.AudioBookSet.GetRecordsAllWithCache().GroupBy(record => record.GroupKey).ToArray();

            foreach (var grouping in records.OrderBy(item => recordGroups[item.Key].Title))
            {
                if (cancellationToken.IsCancellationRequested) return null;

                var recordGroupGroup = new Group(this)
                {
                    LinkedObjectString = grouping.Key,
                    Level = 1,
                    Caption = $"{recordGroups[grouping.Key].Title} [{grouping.Count()}]"
                };

                root.Add(recordGroupGroup);

                var authorRecords = grouping
                    .GroupBy(record => record.Author)
                    .OrderByDescending(item => item.Count())
                    .ThenBy(item => item.Key)
                    .ToArray();

                foreach (var authorRecord in authorRecords)
                {
                    if (cancellationToken.IsCancellationRequested) return null;

                    var groupCaption = $"{authorRecord.Key} [{authorRecord.Count()}]";

                    var recordAuthorGroup = new Group(this)
                    {
                        Caption = groupCaption,
                        Level = 2,
                        LinkedObjectString = authorRecord.Key
                    };

                    foreach (var audioBookKey in authorRecord.Select(item => item.Key))
                    {
                        recordAuthorGroup.LinkedRecords.Add(audioBookKey);
                        recordGroupGroup.LinkedRecords.Add(audioBookKey);
                    }

                    recordGroupGroup.Add(recordAuthorGroup);
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
            return "Grouping logic by Forum\\Author (↓Count)";
        }
    }
}