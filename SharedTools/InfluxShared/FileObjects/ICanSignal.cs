using System;

namespace InfluxShared.FileObjects
{
    public interface ICanSignal
    {
        public string Name { get; set; }
        public string Units { get; set; }
        public uint Ident { get; }
        public string IdentHex { get; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public byte ItemType { get; set; }
        public string Comment { get; set; }
        public ushort StartBit { get; set; }
        public ushort BitCount { get; set; }
        public DBCSignalType Type { get; set; }
        public UInt64 Mode { get; set; }   //If the signal is Mode Dependent
        public DBCByteOrder ByteOrder { get; set; }
        public DBCValueType ValueType { get; set; }

        public ItemConversion Conversion { get; set; }
        public double Factor { get; }
        public double Offset { get; }

        public bool EqualProps(object item);
        public ChannelDescriptor GetDescriptor { get; }
        public bool Log { get; set; }
        public byte UDS { get; set; }


    }
}
