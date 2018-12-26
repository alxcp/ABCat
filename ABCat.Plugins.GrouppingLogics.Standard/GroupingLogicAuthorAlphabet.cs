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
    public class GroupingLogicAuthorAlphabet : GroupingLogicPluginBase
    {
        public override string Name => "Автор";

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        protected override Group GenerateGroupsInternal(CancellationToken cancellationToken)
        {
            var dbContainer = Context.I.DbContainer;
            var root = new Group(this) {Caption = "Все группы произведений", Level = 0};
            var records = dbContainer.AudioBookSet.GetRecordsAllWithCache()
                .GroupBy(item => item.Author).ToArray();

            var alphabetGroups = new Dictionary<char, Group>();

            foreach (var record in records.OrderBy(item => item.Key))
            {
                if (cancellationToken.IsCancellationRequested) return null;
                var authorName = record.Key.IsNullOrEmpty() ? " " : record.Key;

                if (!alphabetGroups.TryGetValue(authorName[0], out var alphabetGroup))
                {
                    alphabetGroup = new Group(this)
                    {
                        Caption = authorName[0].ToString(),
                        Level = 1
                    };
                    alphabetGroups.Add(authorName[0], alphabetGroup);
                    root.Add(alphabetGroup);
                }

                if (authorName.Length > 40) authorName = authorName.Substring(0, 37) + "...";
                var authorGroup = new Group(this)
                {
                    Caption = $"{authorName} [{record.Count()}]",
                    Level = 2
                }; //,  LinkedObjectId = record.Key };

                foreach (var audioBookKey in record.Select(item => item.Key))
                {
                    alphabetGroup.LinkedRecords.Add(audioBookKey);
                    authorGroup.LinkedRecords.Add(audioBookKey);
                }

                alphabetGroup.Add(authorGroup);
            }

            foreach (var alphabetGroup in alphabetGroups)
            {
                alphabetGroup.Value.Caption += $" [{alphabetGroup.Value.LinkedRecords.Count}]";
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
            else
            {
                result = dbContainer.AudioBookSet.GetRecordsByKeys(group.LinkedRecords);
            }

            return result;
        }
    }
}