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

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        protected override Group GenerateGroupsInternal(CancellationToken cancellationToken)
        {
            using (var dbContainer = Context.I.CreateDbContainer(false))
            {
                var root = new Group(this) {Parent = null, Caption = "Все группы произведений", Level = 0};

                var recordGroups = dbContainer.AudioBookGroupSet.GetRecordGroupsAll()
                    .ToDictionary(item => item.Key, item => item);
                if (cancellationToken.IsCancellationRequested) return null;
                var records =
                    dbContainer.AudioBookSet.GetRecordsAll().ToArray().GroupBy(record => record.GroupKey).ToArray();

                foreach (var grouping in records.OrderBy(item => recordGroups[item.Key].Title))
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    var recordGroupGroup = new Group(this)
                    {
                        LinkedObjectString = grouping.Key,
                        Parent = root,
                        Level = 1,
                        Caption = $"{recordGroups[grouping.Key].Title} [{grouping.Count()}]"
                    };
                    root.Children.Add(recordGroupGroup);

                    var authorRecords = grouping.GroupBy(record => record.Author).ToArray();

                    foreach (
                        var authorRecord in
                        authorRecords.OrderByDescending(item => item.Count()).ThenBy(item => item.Key))
                    {
                        if (cancellationToken.IsCancellationRequested) return null;

                        var groupCaption = $"{authorRecord.Key} [{authorRecord.Count()}]";

                        var recordAuthorGroup = new Group(this)
                        {
                            Parent = recordGroupGroup,
                            Caption = groupCaption,
                            Level = 2,
                            LinkedObjectString = authorRecord.Key
                        };

                        foreach (var audioBookKey in authorRecord.Select(item => item.Key))
                            recordAuthorGroup.LinkedRecords.Add(audioBookKey);

                        recordGroupGroup.Children.Add(recordAuthorGroup);
                    }
                }

                return root;
            }
        }

        protected override IEnumerable<IAudioBook> GetRecordsInner(IDbContainer dbContainer, Group group,
            CancellationToken cancellationToken)
        {
            IEnumerable<IAudioBook> result;

            if (group == null || group.Level == 0)
            {
                result = dbContainer.AudioBookSet.GetRecordsAll().ToArray();
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