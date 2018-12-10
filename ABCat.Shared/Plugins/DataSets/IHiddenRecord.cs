namespace ABCat.Shared.Plugins.DataSets
{
    public interface IHiddenRecord
    {
        string RecordGroupKey { get; set; }
        string RecordKey { get; set; }
    }
}