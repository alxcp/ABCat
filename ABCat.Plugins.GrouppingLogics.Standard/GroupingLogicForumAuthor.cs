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
    public class GroupingLogicForumAuthor : GroupingLogicPluginBase
    {
        public override string Name => "Форум → Автор";

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        protected override Group GenerateGroupsInternal(CancellationToken cancellationToken)
        {
            var root = new Group(this) { Parent = null, Caption = "Все группы", Level = 0 };

            Dictionary<string, IAudioBookGroup> recordGroups;
            Dictionary<int, IAudioBookWebSite> webSites;
            Dictionary<string, IAudioBook[]> recordsByGroup;

            using (var dbContainer = Context.I.CreateDbContainer(false))
            {
                recordGroups = dbContainer.AudioBookGroupSet.GetRecordGroupsAll()
                    .ToDictionary(item => item.Key, item => item);
                cancellationToken.ThrowIfCancellationRequested();
                recordsByGroup = dbContainer.AudioBookSet.GetRecordsAll().ToArray().GroupBy(record => record.GroupKey)
                    .ToDictionary(item => item.Key, item => item.ToArray());

                webSites = dbContainer.WebSiteSet.GetWebSitesAll().ToDictionary(item => item.Id, item => item);
            }

            var orderedRecords = recordsByGroup.Where(item => item.Key != null)
                .OrderBy(item => recordGroups[item.Key].Title);

            foreach (var grouping in orderedRecords)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var recordGroup = recordGroups[grouping.Key];
                var webSiteGroup = root.Children.OfType<WebSiteGroup>()
                    .FirstOrDefault(item => item.WebSite.Id == recordGroup.WebSiteId);

                if (webSiteGroup == null)
                {
                    webSiteGroup = new WebSiteGroup(this, webSites[recordGroup.WebSiteId])
                    {
                        Parent = root,
                        Level = 1,
                        Caption = webSites[recordGroup.WebSiteId].DisplayName
                    };

                    root.Children.Add(webSiteGroup);
                }

                var recordGroupGroup = new Group(this)
                {
                    LinkedObjectString = grouping.Key,
                    Parent = webSiteGroup,
                    Level = 2,
                    Caption = $"{recordGroup.Title} [{grouping.Value.Length}]"
                };
                webSiteGroup.Children.Add(recordGroupGroup);

                var authorRecords = grouping.Value.GroupBy(record => record.Author).ToArray();

                foreach (var authorRecord in authorRecords.OrderBy(item => item.Key))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var groupCaption = $"{authorRecord.Key} [{authorRecord.Count()}]";

                    var recordAuthorGroup = new Group(this)
                    {
                        Parent = recordGroupGroup,
                        Caption = groupCaption,
                        Level = 3
                    };
                    foreach (var audioBookKey in authorRecord.Select(item => item.Key))
                        recordAuthorGroup.LinkedRecords.Add(audioBookKey);
                    recordGroupGroup.Children.Add(recordAuthorGroup);
                }
            }

            return root;

        }

        protected override IEnumerable<IAudioBook> GetRecordsInner(IDbContainer dbContainer, Group group,
            CancellationToken cancellationToken)
        {
            if (group == null || group.Level == 0)
                return dbContainer.AudioBookSet.GetRecordsAll().ToArray();

            if (@group.Level == 1)
                return dbContainer.AudioBookSet.GetRecordsByWebSite(((WebSiteGroup)@group).WebSite.Id).ToArray();

            if (@group.Level == 2)
                return dbContainer.AudioBookSet.GetRecordsByGroup(@group.LinkedObjectString).ToArray();

            return dbContainer.AudioBookSet.GetRecordsByKeys(@group.LinkedRecords).ToArray();
        }
    }
}