using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ABCat.Shared;
using ABCat.Shared.Plugins.Catalog.GroupingLogics;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Group = ABCat.Shared.Plugins.Catalog.GroupingLogics.Group;

namespace ABCat.Plugins.GroupingLogics.Standard
{
    public abstract class GroupingLogicByPropertyAlphabet : GroupingLogicPluginBase
    {
        private readonly Regex _isLetterRegex = new Regex("[A-Za-zА-Яа-я]+");

        protected abstract IReadOnlyCollection<string> GetPropertyValues(IAudioBook audioBook);

        protected override Group GenerateGroupsInternal(CancellationToken cancellationToken)
        {
            var dbContainer = Context.I.DbContainer;
            var root = new Group(this) {Caption = RootGroupCaption, Level = 0};

            var records = dbContainer.AudioBookSet.GetRecordsAllWithCache();
            var recordsByProperty = new Dictionary<string, Group>(StringComparer.InvariantCultureIgnoreCase);
            var alphabetGroups = new Dictionary<string, Group>(StringComparer.InvariantCultureIgnoreCase);

            var emptyPropertyGroup = new Group(this)
            {
                Caption = ValueNotSetCaption,
                Level = 1
            };
            root.Add(emptyPropertyGroup);
            alphabetGroups.Add(ValueNotSetCaption, emptyPropertyGroup);

            foreach (var audioBook in records)
            {
                var propertyValues = GetPropertyValues(audioBook);
                if (propertyValues.Any())
                {
                    foreach (var propertyValue in propertyValues)
                    {
                        var firstLetter = propertyValue[0].ToString().ToUpper();
                        var isLetter = _isLetterRegex.IsMatch(firstLetter);

                        if (!isLetter)
                        {
                            firstLetter = OtherValuesCaption;
                        }

                        if (!alphabetGroups.TryGetValue(firstLetter, out var alphabetGroup))
                        {
                            alphabetGroup = new Group(this)
                            {
                                Caption = firstLetter,
                                Level = 1
                            };
                            alphabetGroups.Add(firstLetter.ToUpper(), alphabetGroup);
                            root.Add(alphabetGroup);
                        }

                        if (!recordsByProperty.TryGetValue(propertyValue, out var group))
                        {
                            var propertyValueForDisplay =
                                propertyValue
                                    .TrimToLength(40)
                                    .ChangeCase(Extensions.CaseTypes.AllWords, false, false);

                            group = new Group(this)
                            {
                                Caption = propertyValueForDisplay,
                                Level = 2
                            };

                            recordsByProperty[propertyValue] = group;
                            alphabetGroup.Add(group);
                        }

                        group.LinkedRecords.Add(audioBook.Key);
                        alphabetGroup.LinkedRecords.Add(audioBook.Key);
                    }
                }
                else
                {
                    emptyPropertyGroup.LinkedRecords.Add(audioBook.Key);
                }
            }

            foreach (var propertyValueGroup in recordsByProperty)
            {
                propertyValueGroup.Value.Caption =
                    $"{propertyValueGroup.Value.Caption} [{propertyValueGroup.Value.LinkedRecords.Count}]";
            }

            foreach (var alphabetGroup in alphabetGroups)
            {
                alphabetGroup.Value.Caption += $" [{alphabetGroup.Value.LinkedRecords.Count}]";
                alphabetGroup.Value.OrderByCaption();
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