using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Models
{
    public class EventData
    {
        public string DeviceSerialNumber { get; set; }
        public string DeviceType { get; set; }
        public string LocalSessionId { get; set; }
        public string EntryId { get; set; }
        public string EntryKey { get; set; }
        public string EntryTimestamp { get; set; }
        public string EntryValue { get; set; }

        public string DeviceTypeVersion { get; set; }
        public string SessionId { get; set; }

        public override string ToString()
        {
            return LocalSessionId + SessionId + DeviceSerialNumber + DeviceType + EntryId + EntryKey + EntryValue + EntryTimestamp;
        }
    }
}
