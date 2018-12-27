using System.Collections.Generic;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.GroupingLogics.Standard
{
    [SingletoneComponentInfo("2.2")]
    [UsedImplicitly]
    public class GroupingLogicAuthorAlphabet : GroupingLogicByPropertyAlphabet
    {
        public override string Name => "Автор";

        protected override IReadOnlyCollection<string> GetPropertyValues(IAudioBook audioBook)
        {
            return audioBook.GetAuthors();
        }
    }
}