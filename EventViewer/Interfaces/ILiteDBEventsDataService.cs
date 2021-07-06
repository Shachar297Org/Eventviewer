using EventViewer.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Interfaces
{
    public interface ILiteDBEventsDataService
    {
        IEnumerable<EventData> GetEventsData(string deviceSerialNumber, string deviceType);
        void Upsert(EventData user);

        void Clear(string localSession);

        void UpsertEventsData(IEnumerable<EventData> eventsData);

        ILiteQueryable<EventData> Query();
        IQueryable<EventData> GetEventsQueryable();

        string InsertSession(Session session);
        Session GetSession(string id);
        int DeleteExpiredSessions();
        bool UpdateSession(string id, Session session);

        bool UpdateSessionTime(string id);
    }
}
