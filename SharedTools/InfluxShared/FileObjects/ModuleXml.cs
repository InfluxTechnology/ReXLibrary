using InfluxShared.Generic;
using InfluxShared.Helpers;
using InfluxShared.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace InfluxShared.FileObjects
{
    [Serializable]
    [XmlRoot("REXMODULE", Namespace = "http://www.influxtechnology.com/xml/ReXModule")]
    public class ModuleXml
    {
        [XmlElement("CONFIG")]
        public Config Config { get; set; }

        [XmlElement("CONFIG_ITEM_LIST")]
        public ConfigItemList ConfigItemList { get; set; }

        [XmlElement("PERIODIC_ITEM_LIST")]
        public PeriodicItemList PeriodicItemList { get; set; }

        [XmlElement("POLLING_ITEM_LIST")]
        public PollingItemList PollingItemList { get; set; }

        public ModuleXml()
        {
            PollingItemList = new();
            PeriodicItemList = new();
            ConfigItemList = new ConfigItemList();
            Config = new Config();
        }

        public static ModuleXml ReadFile(string xmlPath)
        {
            // Deserialize the XML file into RexModule object
            XmlSerializer serializer = new XmlSerializer(typeof(ModuleXml));
            using (FileStream stream = new FileStream(xmlPath, FileMode.Open))
            {
                return (ModuleXml)serializer.Deserialize(stream);
            }
        }
        public static ModuleXml ReadStream(Stream stream)
        {
            // Deserialize the XML file into RexModule object
            XmlSerializer serializer = new XmlSerializer(typeof(ModuleXml));
            return (ModuleXml)serializer.Deserialize(stream);
        }

        public static bool ValidateXml(string xmlPath, string schemaPath, out string error)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;

            settings.Schemas.Add(null, schemaPath);
            using (XmlReader reader = XmlReader.Create(xmlPath, settings))
            {
                try
                {
                    while (reader.Read()) { }
                    error = "XML file is valid against the schema.";
                    return true;
                }
                catch (XmlException ex)
                {
                    error = $"XML exception: {ex.Message}";
                    return false;
                }
                catch (XmlSchemaValidationException ex)
                {
                    error = $"Schema validation exception: {ex.Message}";
                    return false;
                }
            }
        }
    }

    public static class ModuleXmlHelper
    {
        public static void ToFile(this ModuleXml moduleXml, string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ModuleXml));
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                serializer.Serialize(stream, moduleXml);
            }
        }
        public static Stream ToStream(this ModuleXml moduleXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ModuleXml));
            Stream stream = new MemoryStream();
            serializer.Serialize(stream, moduleXml);
            stream.Position = 0;
            return stream;
        }
    }

    public class PollingItemList
    {
        public PollingItemList() { Items = new List<PollingItem>(); }
        [XmlElement("POLLING_ITEM")]
        public List<PollingItem> Items { get; set; }
    }

    public class PeriodicItemList
    {
        public PeriodicItemList() { Items = new List<PeriodicItem>(); }
        [XmlElement("PERIODIC_ITEM")]
        public List<PeriodicItem> Items { get; set; }
    }

    public class Config
    {
        [XmlElement("CAN_BUS")]
        public byte CanBus { get; set; }
        [XmlElement("GUID")]
        public string Guid { get; set; }
        [XmlElement("NAME")]
        public string Name { get; set; }
        [XmlElement("VERSION")]
        public byte Version { get; set; }
    }

    public class ConfigItemList
    {
        public ConfigItemList() { Items = new List<ConfigItem>(); }
        [XmlElement("CONFIG_ITEM")]
        public List<ConfigItem> Items { get; set; }
    }

    public class ConfigItem
    {

        [XmlIgnore]
        public byte[] Data { get; set; }
        [XmlElement("DATA")]
        public string DataHex
        {
            get => Bytes.ToHexBinary(Data);
            set => Data = Bytes.FromHexBinary(value);
        }

        [XmlElement("DELAY")]
        public long Delay { get; set; }
        [XmlElement("ORDER")]
        public byte Order { get; set; }
        [XmlElement("TX_IDENT")]
        public uint TxIdent { get; set; }
        [XmlElement("RX_IDENT")]
        public uint RxIdent { get; set; }
    }

    public class Item : ICanSignal
    {
        [XmlElement("BIT_COUNT")]
        public ushort BitCount { get; set; }

        [XmlElement("DATA_TYPE")]
        public string DataType
        {
            get => ValueType.ToString().ToUpper();
            set
            {
                if (Enum.TryParse(value, true, out DBCValueType VT))
                    ValueType = VT;
            }
        }

        [XmlElement("ENDIAN")]
        public string Endian
        {
            get => ByteOrder.ToString().ToUpper();
            set
            {
                if (Enum.TryParse(value, true, out DBCByteOrder BO))
                    ByteOrder = BO;
            }
        }

        [XmlElement("FACTOR")]
        public double Factor
        {
            get => Conversion.Type.HasFlag(ConversionType.Formula) ? Conversion.Formula.CoeffB : 1;
            set
            {
                Conversion.Type = ConversionType.Formula;
                Conversion.Formula.CoeffB = value;
            }
        }

        [XmlElement("MAXIMUM")]
        public double MaxValue { get; set; }

        [XmlElement("MINIMUM")]
        public double MinValue { get; set; }

        [XmlElement("NAME")]
        public string Name { get; set; }

        [XmlElement("OFFSET")]
        public double Offset
        {
            get => Conversion.Type.HasFlag(ConversionType.Formula) ? Conversion.Formula.CoeffC : 1;
            set
            {
                Conversion.Type = ConversionType.Formula;
                Conversion.Formula.CoeffC = value;
            }
        }

        [XmlElement("SERVICE_IDENT")]
        public uint Ident { get; set; }

        [XmlElement("START_BIT")]
        public ushort StartBit { get; set; }

        [XmlElement("UNITS")]
        public string Units { get; set; }

        public string IdentHex => "0x" + Ident.ToString("X4");

        [XmlIgnore]
        public byte ItemType { get; set; }
        [XmlIgnore]
        public string Comment { get; set; }
        [XmlIgnore]
        public DBCSignalType Type { get; set; }
        [XmlIgnore]
        public DBCByteOrder ByteOrder { get; set; }

        [XmlIgnore]
        public UInt64 Mode { get; set; }
        [XmlIgnore]
        public DBCValueType ValueType { get; set; }

        [XmlIgnore]
        public ItemConversion Conversion { get; set; } = new();

        public ChannelDescriptor GetDescriptor => new ChannelDescriptor()
        {
            StartBit = StartBit,
            BitCount = BitCount,
            isIntel = ByteOrder == DBCByteOrder.Intel,
            HexType = BinaryData.BinaryTypes[(int)ValueType],
            conversionType = Conversion.Type,
            Factor = Factor,
            Offset = Offset,
            Table = null,
            Name = Name,
            Units = Units
        };
        [XmlIgnore]
        public bool Log { get; set; }
        [XmlIgnore]
        public byte UDS { get; set; }
        [XmlIgnore]
        public object TagObject { get; set; }

        public bool EqualProps(object item)
        {
            throw new NotImplementedException();
        }
    }

    public class PeriodicItem : Item
    {
        [XmlElement("MODE_BIT_COUNT")]
        public byte ModeBitCount { get; set; }
        [XmlElement("MODE_START_BIT")]
        public short ModeStartBit { get; set; }
        [XmlElement("MODE_VALUE")]
        public int ModeValue { get; set; }
    }

    public class PollingItem : Item
    {
        [XmlElement("DELAY")]
        public long Delay { get; set; }

        [XmlElement("ORDER")]
        public byte Order { get; set; }

        [XmlElement("SERVICE")]
        public string Service { get; set; }

        [XmlElement("TX_IDENT")]
        public uint TxIdent { get; set; }

        [XmlElement("RX_IDENT")]
        public uint RxIdent { get; set; }

        [XmlElement("UDS_SERVICE_ID")]
        public byte UDSServiceID { get => UDS; set => UDS = value; }

        public PollingItem() { }

        public PollingItem(ICanSignal msg) => msg.CopyProperties(this);
    }


}
