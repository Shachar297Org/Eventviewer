namespace EventViewer.Models
{
    public class CommandData : LumXData
    {
        public string MessageId { get; set; }
        public string CommandName { get; set; }
        public string EntryKey { get; set; }
        public string Timestamp { get; set; }
        public string EntryValue { get; set; }
        public string LocalEntryTimestamp { get; set; }
        public string SessionId { get; set; }

        public override string ToString()
        {
            return LocalSessionId + SessionId + DeviceSerialNumber + DeviceType + MessageId + CommandName + EntryKey + EntryValue + Timestamp + LocalEntryTimestamp;
        }
    }
}
