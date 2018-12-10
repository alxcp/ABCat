namespace ABCat.Shared.Plugins.DataSets
{
    public interface IUserData
    {
        string LocalPath { get; set; }
        string RecordGroupKey { get; set; }
        string RecordKey { get; set; }
    }
}