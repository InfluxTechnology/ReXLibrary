using RXD.Blocks;
using RXD.DataRecords;
using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Base.FrameCollectors
{
    internal class ModeFrameCollector : IFrameCollector
    {
        static MessageFlags FlagsMask = MessageFlags.IDE | MessageFlags.EDL | MessageFlags.BRS | MessageFlags.SRR | MessageFlags.DIR;
        static MessageFlags FlagsTxValue = MessageFlags.DIR;
        static MessageFlags FlagsRxValue = 0;

        static byte FrameTypeMask = 0xF0;
        static byte FrameSequenceIdMask = 0x0F;
        static byte SingleFrame = 0x00;
        static byte FirstFrame = 0x10;
        static byte ConsecutiveFrame = 0x20;
        static byte FlowControlFrame = 0x30;

        static byte ModeRespSID = 0x40;
        static byte NegativeResponse = 0x7F;

        internal class MultiFrameData : RecordCollection
        {
            public UInt32 Tx;
            public UInt32 Rx;
            public byte Mode;
            internal int DataSize;
            internal int TargetCount;

            public RecCanTrace PackUDS()
            {
                RecCanTrace rec = new RecCanTrace
                {
                    header = new RecHeader()
                    {
                        UID = this[0].header.UID,
                        InfSize = this[0].header.InfSize,
                        DLC = (byte)DataSize
                    },
                    LinkedBin = this[0].LinkedBin,
                    BusChannel = this[0].BusChannel,
                    NotExportable = true,
                    NotVisible = true,
                    CustomType = PackType.UDS,
                };

                rec.data.Timestamp = (this[Count - 1] as RecCanTrace).data.Timestamp;
                rec.data.Flags = (this[1] as RecCanTrace).data.Flags;
                rec.data.CanID = (this[1] as RecCanTrace).data.CanID;

                UInt16 OutOffset = 0;
                UInt16 InOffset = 0;

                UInt16 Copy(Array sourceArray, int sourceIndex, Array destinationArray, int length)
                {
                    Array.Copy(sourceArray, sourceIndex, destinationArray, OutOffset, length);
                    OutOffset += (UInt16)length;
                    return (UInt16)length;
                }

                UInt16 txItemSize = 0;
                byte CanRxLengthSize = (byte)(Count > 2 ? 2 : 1);
                if (Mode == 0x22)
                {
                    rec.CustomType = PackType.UDS22;
                    rec.header.DLC += 2;
                    rec.VariableData = new byte[rec.header.DLC];
                    InOffset += Copy(this[1].VariableData, CanRxLengthSize, rec.VariableData, 3);
                    rec.VariableData[3] = 0;
                    rec.VariableData[4] = 0;
                    OutOffset += 2;
                    InOffset += Copy(this[1].VariableData, CanRxLengthSize + 3, rec.VariableData, Math.Min(DataSize - InOffset, (UInt16)(5 - CanRxLengthSize)));
                }
                else if (Mode == 0x23)
                {
                    txItemSize = (ushort)(this[0].VariableData[2] & 0x0F);
                    rec.CustomType = PackType.UDS23;
                    rec.header.DLC += (byte)txItemSize;
                    rec.VariableData = new byte[rec.header.DLC];
                    InOffset += Copy(this[1].VariableData, CanRxLengthSize, rec.VariableData, 1);
                    Copy(this[0].VariableData, 3, rec.VariableData, txItemSize);
                    InOffset += Copy(this[1].VariableData, CanRxLengthSize + 1, rec.VariableData, Math.Min(7 - CanRxLengthSize, DataSize - InOffset));
                }
                else
                {
                    rec.VariableData = new byte[rec.header.DLC];
                    InOffset += Copy(this[1].VariableData, CanRxLengthSize, rec.VariableData, Math.Min(DataSize - InOffset, (UInt16)(8 - CanRxLengthSize)));
                }

                for (int i = 2; i < Count; i++)
                    InOffset += Copy(this[i].VariableData, 1, rec.VariableData, Math.Min(7, DataSize - InOffset));

                return rec;
            }
        }

        MultiFrameData data = new();

        public void InitFrame(RecCanTrace msg)
        {
            data.Clear();
            data.Mode = msg.VariableData[1];
            data.Tx = msg.data.CanID;
            data.Rx = 0;
            data.TargetCount = 0;
            data.DataSize = 0;

            data.Add(msg);
        }

        public bool AppendFrame(RecCanTrace msg)
        {
            if (data.Count == 0)
                return false;

            byte FrameType = (byte)(msg.VariableData[0] & FrameTypeMask);
            if (FrameType == FlowControlFrame)
                return true;

            if (data.Count == 1)
            {
                if (FrameType == SingleFrame && msg.VariableData[1] == data.Mode + ModeRespSID)
                {
                    data.DataSize = msg.VariableData[0];
                    data.TargetCount = 2;
                }
                else if (FrameType == FirstFrame && msg.VariableData[2] == data.Mode + ModeRespSID)
                {
                    data.DataSize = (ushort)(((msg.VariableData[0] & 0xF) << 8) + msg.VariableData[0]);
                    data.TargetCount = 1 + (data.DataSize + 6) / 7;
                }
                else
                    return false;

                data.Add(msg);
            }
            else
            {
                if (FrameType == ConsecutiveFrame && (msg.VariableData[0] & FrameSequenceIdMask) == ((1 + data[data.Count - 1].VariableData[0]) & FrameSequenceIdMask))
                    data.Add(msg);
                else 
                    return false;
            }

            return true;
        }

        public bool TryCollect(RecBase record, RXDataReader reader)
        {
            if (record.LinkedBin is null)
                return false;

            if (record.LinkedBin.BinType != BlockType.CANMessage || record.LinkedBin.RecType != RecordType.CanTrace)
                return false;

            RecCanTrace msg = record as RecCanTrace;

            if ((msg.data.Flags & FlagsMask) == FlagsTxValue)
            {
                InitFrame(msg);
                return true;
            }
            else if ((msg.data.Flags & FlagsMask) == FlagsRxValue)
            {
                if (AppendFrame(msg) && data.Count == data.TargetCount)
                {
                    reader.MessageCollection.Insert(reader.MessageCollection.IndexOf(record) + 1, data.PackUDS());
                    data.Clear();
                    return true;
                }
            }

            return false;
        }

    }
}
