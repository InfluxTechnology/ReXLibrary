using InfluxShared.Generic;
using MDF4xx.Frames;
using RXD.Blocks;
using RXD.Objects;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RXD.DataRecords
{
    internal class RecBase
    {
        /// <summary>
        /// BlockType to Class reference dictionary
        /// </summary>
        static readonly Dictionary<RecordType, Type> RecClass = new Dictionary<RecordType, Type>()
        {
            { RecordType.Unknown, typeof(RecBase) },
            { RecordType.PreBuffer, typeof(RecPreBuffer) },
            { RecordType.CanTrace, typeof(RecCanTrace) },
            { RecordType.CanError, typeof(RecCanTraceError) },
            { RecordType.LinTrace, typeof(RecLinTrace) },
            { RecordType.MessageData, typeof(RecMessage) },
        };
        internal RecHeader header;

        /// <summary>
        /// Custom Data block
        /// </summary>
        protected object data;

        /// <summary>
        /// Variable Data block
        /// </summary>
        internal byte[] VariableData;
        protected virtual int VariableDataSize { get => (VariableData == null) ? 0 : VariableData.Length; }

        internal virtual UInt32 RawTimestamp { get; set; }
        internal byte BusChannel;
        internal BinBase LinkedBin = null;

        internal bool NotExportable = false;
        internal bool NotVisible = false;

        public RecBase()
        {

        }

        public byte[] ToBytes()
        {
            int bufsize = Marshal.SizeOf(header) + header.InfSize + header.DLC;
            byte[] buffer = new byte[bufsize];
            Array.Copy(Bytes.ObjectToBytes(header), buffer, Marshal.SizeOf(header));
            Array.Copy(Bytes.ObjectToBytes(data), 0, buffer, Marshal.SizeOf(header), header.InfSize);
            Array.Copy(VariableData, 0, buffer, Marshal.SizeOf(header) + header.InfSize, VariableDataSize);
            return buffer;
        }

        public virtual List<BaseDataFrame> ToMdfFrame() => new List<BaseDataFrame>();

        public virtual MessageFrame ConvertToMdfMessageFrame(UInt16 GroupID, byte DLC) => new MessageFrame();

        public virtual TraceCollection ToTraceRow(UInt32 TimestampPrecision)
        {
            /*TraceRow trace = new TraceRow()
            {
                TraceType = RecordType.Unknown,
                _CanID = header.UID,
                _DLC = header.DLC,
                _Data = new byte[header.DLC]
            };

            Buffer.BlockCopy(VariableData, 0, trace._Data, 0, header.DLC);*/

            return new TraceCollection()
            {
                //trace
            };
        }

        public static RecBase Parse(RecRaw input)
        {
            // Detect type
            RecBase rec = (RecBase)Activator.CreateInstance(RecClass[input.LinkedBin.RecType]);

            // Set Header
            rec.header = input.header;

            // Set Base Data
            GCHandle h = GCHandle.Alloc(input.Data, GCHandleType.Pinned);
            Marshal.PtrToStructure(h.AddrOfPinnedObject(), rec.data);
            h.Free();

            // Set Var Data
            rec.VariableData = input.VariableData;

            // Set Bus channel
            rec.BusChannel = input.BusChannel;

            rec.LinkedBin = input.LinkedBin;

            return rec;
        }
    }
}
