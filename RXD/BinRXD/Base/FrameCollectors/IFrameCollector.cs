using RXD.DataRecords;

namespace RXD.Base.FrameCollectors
{
    internal interface IFrameCollector
    {
        public bool TryCollect(RecBase record, RXDataReader reader);
    }
}
