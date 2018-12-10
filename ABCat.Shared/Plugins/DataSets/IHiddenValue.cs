namespace ABCat.Shared.Plugins.DataSets
{
    public interface IHiddenValue
    {
        bool IgnoreCase { get; set; }
        string PropertyName { get; set; }
        string Value { get; set; }
        bool WholeWord { get; set; }
    }
}