using System.Collections.Generic;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.GroupingLogics.Standard
{
    [SingletoneComponentInfo("2.2")]
    [UsedImplicitly]
    public class GroupingLogicGenreAlphabet : GroupingLogicByPropertyAlphabet
    {
        public override string Name => "Жанр";

        protected override IReadOnlyCollection<string> GetPropertyValues(IAudioBook audioBook)
        {
            return audioBook.GetGenres();
        }
    }
}