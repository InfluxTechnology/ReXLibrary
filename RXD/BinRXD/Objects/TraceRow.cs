using System;

namespace RXD.Objects
{
    public class TraceRow : IRecordTimeAdapter
    {
        public bool NotExportable;

        public UInt32 RawTimestamp { get; set; }

        public double FloatTimestamp { get; set; }
    }
}
