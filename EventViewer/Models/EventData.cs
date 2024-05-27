﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Models
{
    public class EventData : LumXData
    {
        public string EntryId { get; set; }
        public string EntryKey { get; set; }
        public string EntryTimestamp { get; set; }
        public string EntryValue { get; set; }
        public string LocalEntryTimestamp { get; set; }
        public string DeviceTypeVersion { get; set; }
        public string SessionId { get; set; }

        public override string ToString()
        {
            return LocalSessionId + SessionId + DeviceSerialNumber + DeviceType + EntryId + EntryKey + EntryValue + EntryTimestamp + LocalEntryTimestamp;
        }
    }
}
