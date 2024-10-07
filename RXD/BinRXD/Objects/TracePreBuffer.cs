namespace RXD.Objects
{
    internal class TracePreBuffer : TraceRow, IRecordGridItem
    {
        public string strTimestamp => ""; //string.Format("{0:0.000000}", FloatTimestamp);

        public string strBusChannel => "";

        public string strCanID => "";

        public string strFlags => "";

        public string strDLC => "";

        public string strData { get; set; } = "Trigger event";
    }
}
