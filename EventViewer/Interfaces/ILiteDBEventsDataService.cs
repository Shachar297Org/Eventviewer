using EventViewer.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Interfaces
{
    public interface ILiteDBDataService
    {
        IEnumerable<EventData> GetEventsData(string deviceSerialNumber, string deviceType);
        IEnumerable<CommandData> GetCommandsData(string deviceSerialNumber, string deviceType);

        void Clear<T>(string localSession) where T : LumXData;

        void UpsertEventsData(IEnumerable<EventData> eventsData);
        void UpsertCommandsData(IEnumerable<CommandData> eventsData);

        IQueryable<EventData> GetEventsQueryable();
        IQueryable<CommandData> GetCommandsQueryable();

        string InsertSession(Session session);
        Session GetSession(string id); 
        int DeleteExpiredSessions();
        bool UpdateSession(string id, Session session);

        bool UpdateSessionTime(string id);

        bool SessionIsExpired(string sessionId);

        string GetIdForUser(User sessionUser);

    }
}
