using System.Collections.Generic;
using JetBrains.Annotations;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IReplacementStringSet : IObjectSet<IReplacementString>
    {
        void AddReplacementString(params IReplacementString[] replacementString);
        IReplacementString CreateReplacementString();

        void Delete([NotNull] string recordPropertyName, [NotNull] string replaceValue,
            [CanBeNull] string possibleValue);

        IEnumerable<IReplacementString> GetReplacementStringsAll();
        IEnumerable<IReplacementString> GetReplacementStringsBy(string propertyName);
    }
}