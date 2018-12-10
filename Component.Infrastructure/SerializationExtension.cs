using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace Component.Infrastructure
{
    public static class SerializationExtension
    {
        public static T DeserializeFromBinaryBytes<T>(this byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return ms.DeserializeFromBinaryStream<T>();
            }
        }

        public static T DeserializeFromBinaryFile<T>(this FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                return fs.DeserializeFromBinaryStream<T>();
            }
        }

        public static T DeserializeFromBinaryStream<T>(this Stream stream)
        {
            var binarySerializer = new BinaryFormatter();
            return (T) binarySerializer.Deserialize(stream);
        }

        public static T DeserializeFromXmlBytes<T>(this byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return ms.DeserializeFromXmlStream<T>();
            }
        }

        public static T DeserializeFromXmlStream<T>(this Stream stream)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            return (T) xmlSerializer.Deserialize(stream);
        }

        public static object DeserializeFromXmlString(this string xml, Type type)
        {
            using (var sr = new StringReader(xml))
            {
                var xs = new XmlSerializer(type);
                return xs.Deserialize(sr);
            }
        }

        public static T DeserializeFromXmlString<T>(this string xml)
        {
            using (var sr = new StringReader(xml))
            {
                var xs = new XmlSerializer(typeof(T));
                return (T) xs.Deserialize(sr);
            }
        }

        public static T DeserializeXmlFromFile<T>(this FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                return fs.DeserializeFromXmlStream<T>();
            }
        }

        public static byte[] SerializeToBinaryBytes(this object o)
        {
            using (var ms = new MemoryStream())
            {
                SerializeToBinaryStream(o, ms);
                return ms.ToArray();
            }
        }

        public static void SerializeToBinaryFile(this object o, string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                o.SerializeToBinaryStream(fs);
            }
        }

        public static void SerializeToBinaryStream(this object o, Stream stream)
        {
            var binarySerializer = new BinaryFormatter();
            binarySerializer.Serialize(stream, o);
        }

        public static byte[] SerializeToXmlBytes(this object o)
        {
            using (var ms = new MemoryStream())
            {
                o.SerializeToXmlStream(ms);
                return ms.ToArray();
            }
        }

        public static void SerializeToXmlFile(this object o, string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                o.SerializeToXmlStream(fs);
            }
        }

        public static void SerializeToXmlStream(this object o, Stream stream)
        {
            var xmlSerializer = new XmlSerializer(o.GetType());
            xmlSerializer.Serialize(stream, o);
        }

        public static string SerializeToXmlString(this object o)
        {
            using (var sw = new StringWriter())
            {
                var xs = new XmlSerializer(o.GetType());
                xs.Serialize(sw, o);
                return sw.ToString();
            }
        }
    }
}