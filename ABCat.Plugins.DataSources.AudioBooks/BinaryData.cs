using ABCat.Shared.Plugins.DataSets;
using Ionic.Zlib;
using SQLite.Net.Attributes;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    [Table("BinaryData")]
    public class BinaryData : IBinaryData
    {
        [Column("Data")] public byte[] Data { get; set; }

        public bool IsCompressed { get; set; }

        [PrimaryKey] public string Key { get; set; }

        public byte[] GetData()
        {
            if (IsCompressed) return GZipStream.UncompressBuffer(Data);
            return Data;
        }

        public string GetString()
        {
            if (IsCompressed) return Context.I.DefaultEncoding.GetString(GZipStream.UncompressBuffer(Data));
            return Context.I.DefaultEncoding.GetString(Data);
        }

        public void SetData(byte[] data, bool compress)
        {
            IsCompressed = compress;
            if (compress) Data = GZipStream.CompressBuffer(data);
            else Data = data;
        }

        public void SetString(string data, bool compress)
        {
            IsCompressed = compress;
            if (compress) Data = GZipStream.CompressBuffer(Context.I.DefaultEncoding.GetBytes(data));
            else Data = Context.I.DefaultEncoding.GetBytes(data);
        }
    }
}