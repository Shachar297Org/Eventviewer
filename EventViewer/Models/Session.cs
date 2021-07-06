using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Models
{
    public class Session
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string SessionId { get; set; }
        public DateTime StartTime { get; set; }
        public User User { get; set; }
    }
}
