﻿using InfluxShared.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InfluxShared.FileObjects
{
    public enum DBCByteOrder : byte { Intel, Motorola }
    public enum DBCMessageType : byte { Standard, Extended, CanFDStandard, CanFDExtended, J1939PG, Lin, KanCan, reserved }
    public enum DBCValueType : byte { Unsigned, Signed, IEEEFloat, IEEEDouble, ASCII, BYTES }
    public enum DBCSignalType : byte { Standard, Mode, ModeDependent }

    /* Unmerged change from project 'Oscilloscope'
    Before:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    After:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    */

    /* Unmerged change from project 'RXDDemo'
    Before:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    After:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    */

    /* Unmerged change from project 'ReXusbcanDemo'
    Before:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    After:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    */

    /* Unmerged change from project 'ModuleConfigurator'
    Before:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    After:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    */

    /* Unmerged change from project 'ReXdeskConvert'
    Before:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    After:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    */

    /* Unmerged change from project 'RxLibrary'
    Before:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    After:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    */

    /* Unmerged change from project 'RxdToolkit'
    Before:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    After:
        public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



        public class DbcSelection
    */
    public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };



    public class DbcSelection
    {
        public bool Log { get; set; }
        public DbcItem Item { get; set; }
    }

    public class DbcItem : BasicItemInfo, ICanSignal
    {
        public ushort StartBit { get; set; }
        public ushort BitCount { get; set; }
        public DBCSignalType Type { get; set; }
        public UInt32 Mode { get; set; }   //If the signal is Mode Dependent
        public DBCByteOrder ByteOrder { get; set; }
        public DBCValueType ValueType { get; set; }
        public bool Log { get; set; }
        public override string ToString() => Name;
        public double Factor => Conversion.Type.HasFlag(ConversionType.Formula) ? Conversion.Formula.CoeffB : 1;
        public double Offset => Conversion.Type.HasFlag(ConversionType.Formula) ? Conversion.Formula.CoeffC : 0;
        public TableNumericConversion Table => Conversion.Type.HasFlag(ConversionType.TableNumeric) ? Conversion.TableNumeric : null;

        internal DbcMessage Parent { get; set; }
        public override uint Ident => Parent?.CANID ?? 0;
        public override string IdentHex => Parent?.HexIdent ?? "";

        public bool EqualProps(object item) =>
            item is DbcItem dbc &&
            dbc.StartBit == StartBit &&
            dbc.BitCount == BitCount &&
            dbc.Type == Type &&
            dbc.Mode == Mode &&
            dbc.ByteOrder == ByteOrder &&
            dbc.ValueType == ValueType;

        public DbcItem Clone => (DbcItem)MemberwiseClone();

        public override ChannelDescriptor GetDescriptor => new ChannelDescriptor()
        {
            StartBit = StartBit,
            BitCount = BitCount,
            isIntel = ByteOrder == DBCByteOrder.Intel,
            HexType = BinaryData.BinaryTypes[(int)ValueType],
            conversionType = Conversion.Type,
            Factor = Factor,
            Offset = Offset,
            Table = Table,
            Name = Name,
            Units = Units
        };
    }

    public class DbcMessage : ICanMessage
    {
        public string Name { get; set; }
        public uint CANID { get; set; }
        public string HexIdent => "0x" + (isExtended ? CANID.ToString("X8") : CANID.ToString("X3"));
        public byte DLC { get; set; }
        public DBCMessageType MsgType { get; set; }
        public bool isExtended => MsgType == DBCMessageType.Extended || MsgType == DBCMessageType.CanFDExtended || MsgType == DBCMessageType.J1939PG;
        public string Transmitter { get; set; }
        public string Comment { get; set; }
        public List<ICanSignal> Signals => Items.Cast<ICanSignal>().ToList();
        public List<DbcItem> Items { get; set; }
        public bool Log { get; set; }

        public uint FullID => isExtended ? (uint)(CANID | (1 << 31)) : CANID;

        public bool EqualProps(object obj) =>
            obj is not null && obj is DbcMessage msg &&
            msg.MsgType == MsgType &&
            msg.CANID == CANID;

        public DbcMessage()
        {
            Items = new List<DbcItem>();
        }
    }

    public class DBC
    {
        public DBCFileType FileType { get; set; }
        public string FileName { get; set; }
        public string FileNameSerialized { get; set; }  //Imeto na DBC-to zapisano kato serialized file
        public string FileNameNoExt => Path.GetFileNameWithoutExtension(FileName);
        public string FileLocation => Path.GetDirectoryName(FileName);
        public List<DbcMessage> Messages { get; set; }

        public bool Equals(DBC dbc)
        {
            if (this != null && dbc != null)
            {
                if (this.FileNameSerialized == dbc.FileNameSerialized)
                    return true;
            }
            return false;
        }

        public DBC()
        {
            Messages = new List<DbcMessage>();
        }

        public void AddToReferenceCollection(ReferenceCollection collection, byte BusChannel)
        {
            foreach (var msg in Messages)
                foreach (var sig in msg.Items)
                    collection.Add(new ReferenceDbcChannel()
                    {
                        BusChannelIndex = BusChannel,
                        FileName = FileNameSerialized,
                        MessageID = msg.FullID,
                        SignalName = sig.Name
                    });
        }
    }

    public class ExportDbcMessage
    {
        public UInt64 uniqueid { get; set; }
        public byte BusChannel { get; set; }
        public DbcMessage Message { get; set; }
        public List<ICanSignal> Signals { get; set; }

        public ICanSignal multiplexor = null;
        public BinaryData multiplexorData = null;
        // Multiplexor map is dictionary with pair - mode value and list of signal indexes
        public Dictionary<UInt16, List<UInt16>> multiplexorMap = null;
        // Mode dependant group ids
        public List<UInt64> multiplexorGroups = null;

        public static bool operator ==(ExportDbcMessage item1, ExportDbcMessage item2) => item1.BusChannel == item2.BusChannel && item1.Message.EqualProps(item2.Message);
        public static bool operator !=(ExportDbcMessage item1, ExportDbcMessage item2) => !(item1 == item2);
        public override bool Equals(object obj)
        {
            if (obj is ExportDbcMessage)
                return this == (ExportDbcMessage)obj;
            else
                return false;
        }

        public void AddSignal(ICanSignal Signal)
        {
            Signals.Add(Signal);
            multiplexor = GetMode();
            if (multiplexor is not null)
                multiplexorData = multiplexor.GetDescriptor.CreateBinaryData();
        }

        public ICanSignal GetMode()
        {
            if (Signals[0].Type == DBCSignalType.Mode)
            {
                multiplexorMap =
                    Signals.Where(sg => sg.Type == DBCSignalType.ModeDependent).
                    GroupBy(md => md.Mode,
                    (k, c) => new { ModeValue = (UInt16)k, Indexes = c.Select(cs => (UInt16)Signals.IndexOf(cs)).ToList() }).
                    ToDictionary(d => d.ModeValue, d => d.Indexes);

                return Signals[0];
            }
            else
            {
                ICanSignal mode = Signals.FirstOrDefault(s => s.Type == DBCSignalType.Mode);
                if (mode is not null)
                {
                    Signals.Remove(mode);
                    Signals.Insert(0, mode);

                    multiplexorMap =
                        Signals.Where(sg => sg.Type == DBCSignalType.ModeDependent).
                        GroupBy(md => md.Mode,
                        (k, c) => new { ModeValue = (UInt16)k, Indexes = c.Select(cs => (UInt16)Signals.IndexOf(cs)).ToList() }).
                        ToDictionary(d => d.ModeValue, d => d.Indexes);
                }
                return mode;
            }
        }

        public UInt16 FindMultiplexorIndex(DbcItem signal)
        {
            var keys = multiplexorMap.Where(pair => pair.Value.Contains((ushort)Signals.IndexOf(signal))).
                Select(pair => pair.Key);

            if (keys.Count() > 0)
                return (UInt16)multiplexorMap.Keys.ToList().IndexOf(keys.First());
            else
                return UInt16.MaxValue;
        }

        public UInt32 CalcInvalidationBitCount => (uint)(multiplexorMap is null ? 0 : (multiplexorMap.Count() + 7) >> 3);
    }

    public class ExportDbcCollection : List<ExportDbcMessage>
    {
        public ExportDbcMessage AddMessage(byte BusChannel, DbcMessage Message)
        {
            foreach (ExportDbcMessage m in this)
                if (m.BusChannel == BusChannel && m.Message.EqualProps(Message))
                    return m;

            ExportDbcMessage channel = new ExportDbcMessage()
            {
                BusChannel = BusChannel,
                Message = Message,
                Signals = new List<ICanSignal>()
            };
            Add(channel);
            return channel;
        }

    }

    public static class DbcHelper
    {
        public static string ToDisplayName(this DBCMessageType msgType)
        {
            switch (msgType)
            {
                case DBCMessageType.Standard: return "CAN Standard";
                case DBCMessageType.Extended: return "CAN Extended";
                case DBCMessageType.CanFDStandard: return "CAN FD Standard";
                case DBCMessageType.CanFDExtended: return "CAN FD Extended";
                case DBCMessageType.J1939PG: return "J1939 PG (ext. ID)";
                case DBCMessageType.Lin: return "Lin";
                case DBCMessageType.KanCan: return "KanCan";
                default: return "Unknown";
            }
        }
    }
}
