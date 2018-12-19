using System;
using System.ComponentModel;

namespace ABCat.Shared.Plugins.Catalog.FilteringLogics
{
    [Serializable]
    public class FilterFields
    {
        public FilterFields()
        {
            Fields = new SerializableDictionary<string, string>();
        }

        public string Name { get; set; }

        [Browsable(false)] public bool IsEmpty => !Fields.AnySafe();

        public SerializableDictionary<string, string> Fields { get; set; }

        public void ClearValue(string key)
        {
            if (Fields.ContainsKey(key))
                Fields.Remove(key);
        }

        public void SetValue(string key, string value)
        {
            if (value.IsNullOrEmpty())
                ClearValue(key);
            else
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