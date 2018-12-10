namespace ABCat.Shared.Plugins.DataSets
{
    public interface IBinaryDataSet : IObjectSet<IBinaryData>
    {
        void AddChangedBinaryData(params IBinaryData[] binaryData);
        IBinaryData CreateBinaryData();
        IBinaryData GetByKey(string key);
        void Import(string dbFilePath);
        void SaveBinaryData();
    }
}