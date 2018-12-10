namespace ABCat.Shared.Plugins.DataSets
{
    public interface IReplacementString
    {
        string PossibleValue { get; set; }
        string RecordPropertyName { get; set; }
        string ReplaceValue { get; set; }
    }
}