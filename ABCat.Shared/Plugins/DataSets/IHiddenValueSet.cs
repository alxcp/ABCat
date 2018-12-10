using System.Collections.Generic;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IHiddenValueSet : IObjectSet<IHiddenValue>
    {
        void AddHiddenValue(params IHiddenValue[] hiddenValue);
        IHiddenValue CreateHiddenValue();
        void Delete(string propertyName, string value);
        IEnumerable<IHiddenValue> GetHiddenValues(string propertyName);
        IEnumerable<IHiddenValue> GetHiddenValuesAll();
        bool IsHidden(string propertyName, string value);
    }
}