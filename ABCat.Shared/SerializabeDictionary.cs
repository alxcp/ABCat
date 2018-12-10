using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

[Serializable]
public sealed class SerializableDictionary<TKey, TVal> : Dictionary<TKey, TVal>, IXmlSerializable, ISerializable
{
    private const string DictionaryNodeName = "Dictionary";
    private const string ItemNodeName = "Item";
    private const string KeyNodeName = "Key";
    private const string ValueNodeName = "Value";
    private XmlSerializer _keySerializer;
    private XmlSerializer _valueSerializer;

    public SerializableDictionary()
    {
    }

    public SerializableDictionary(IDictionary<TKey, TVal> dictionary)
        : base(dictionary)
    {
    }

    public SerializableDictionary(IEqualityComparer<TKey> comparer)
        : base(comparer)
    {
    }

    public SerializableDictionary(int capacity)
        : base(capacity)
    {
    }

    public SerializableDictionary(IDictionary<TKey, TVal> dictionary, IEqualityComparer<TKey> comparer)
        : base(dictionary, comparer)
    {
    }

    public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        : base(capacity, comparer)
    {
    }

    private SerializableDictionary(SerializationInfo info, StreamingContext context)
    {
        var itemCount = info.GetInt32("ItemCount");
        for (var i = 0; i < itemCount; i++)
        {
            var kvp = (KeyValuePair<TKey, TVal>) info.GetValue($"Item{i}",
                typeof(KeyValuePair<TKey, TVal>));
            Add(kvp.Key, kvp.Value);
        }
    }

    private XmlSerializer ValueSerializer
    {
        get
        {
            if (_valueSerializer == null)
            {
                _valueSerializer = new XmlSerializer(typeof(TVal));
            }

            return _valueSerializer;
        }
    }

    private XmlSerializer KeySerializer
    {
        get
        {
            if (_keySerializer == null)
            {
                _keySerializer = new XmlSerializer(typeof(TKey));
            }

            return _keySerializer;
        }
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("ItemCount", Count);
        var itemIdx = 0;
        foreach (var kvp in this)
        {
            info.AddValue($"Item{itemIdx}", kvp, typeof(KeyValuePair<TKey, TVal>));
            itemIdx++;
        }
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        //writer.WriteStartElement(DictionaryNodeName);
        foreach (var kvp in this)
        {
            writer.WriteStartElement(ItemNodeName);
            writer.WriteStartElement(KeyNodeName);
            KeySerializer.Serialize(writer, kvp.Key);
            writer.WriteEndElement();
            writer.WriteStartElement(ValueNodeName);
            ValueSerializer.Serialize(writer, kvp.Value);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        //writer.WriteEndElement();
    }

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return;
        }

        // Move past container
        if (!reader.Read())
        {
            throw new XmlException("Error in Deserialization of Dictionary");
        }

        //reader.ReadStartElement(DictionaryNodeName);
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            reader.ReadStartElement(ItemNodeName);
            reader.ReadStartElement(KeyNodeName);
            var key = (TKey) KeySerializer.Deserialize(reader);
            reader.ReadEndElement();
            reader.ReadStartElement(ValueNodeName);
            var value = (TVal) ValueSerializer.Deserialize(reader);
            reader.ReadEndElement();
            reader.ReadEndElement();
            Add(key, value);
            reader.MoveToContent();
        }
        //reader.ReadEndElement();

        reader.ReadEndElement(); // Read End Element to close Read of containing node
    }

    XmlSchema IXmlSerializable.GetSchema()
    {
        return null;
    }
}