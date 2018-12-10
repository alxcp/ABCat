namespace ABCat.Shared.Plugins.DataSets
{
    public interface IBinaryData
    {
        byte[] Data { get; set; }
        bool IsCompressed { get; set; }
        string Key { get; set; }

        byte[] GetData();
        string GetString();
        void SetData(byte[] page, bool compress);
        void SetString(string page, bool compress);
    }
}