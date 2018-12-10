using System.ComponentModel;

namespace ABCat.Shared.Plugins.Catalog.FilteringLogics
{
    public class FilterFields
    {
        public FilterFields()
        {
            Fields = new SerializableDictionary<string, string>();
        }

        public string Name { get; set; }

        [Browsable(false)] public bool IsEmpty => true;

        public SerializableDictionary<string, string> Fields { get; set; }

        public void SetValue(string key, string value)
        {
            Fields[key] = value;
        }

        public string GetValue(string key)
        {
            Fields.TryGetValue(key, out var value);
            return value;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}