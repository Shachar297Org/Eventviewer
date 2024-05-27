using EventViewer.Interfaces;
using EventViewer.Models;
using JqueryDataTables.ServerSide.AspNetCoreWeb.Models;
using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Services
{
    public class LiteDBDataService : ILiteDBDataService
    {
        private LiteDatabase _liteDb;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IConfiguration Configuration;

        public LiteDBDataService(ILiteDbContext liteDbContext, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _liteDb = liteDbContext.Database;
            _liteDb.Pragma("UTC_DATE", true);
            _liteDb.UtcDate = true;

            _httpContextAccessor = httpContextAccessor;

            Configuration = configuration;

            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            //_liteDb.GetCollection<EventData>("EventData").EnsureIndex("Compound", f => f.ToString(), true);
            _liteDb.GetCollection<EventData>("EventData").EnsureIndex(expression => expression.EntryId);
            _liteDb.GetCollection<EventData>("EventData").EnsureIndex(expression => expression.EntryTimestamp);
            _liteDb.GetCollection<EventData>("EventData").EnsureIndex(expression => expression.EntryKey);
            _liteDb.GetCollection<EventData>("EventData").EnsureIndex(expression => expression.EntryValue);
            _liteDb.GetCollection<EventData>("EventData").EnsureIndex(expression => expression.DeviceSerialNumber);
            _liteDb.GetCollection<EventData>("EventData").EnsureIndex(expression => expression.DeviceType);
            _liteDb.GetCollection<EventData>("EventData").EnsureIndex(expression => expression.LocalEntryTimestamp);

            _liteDb.GetCollection<CommandData>("CommandData").EnsureIndex(expression => expression.CommandName);
            _liteDb.GetCollection<CommandData>("CommandData").EnsureIndex(expression => expression.Timestamp);
            _liteDb.GetCollection<CommandData>("CommandData").EnsureIndex(expression => expression.EntryKey);
            _liteDb.GetCollection<CommandData>("CommandData").EnsureIndex(expression => expression.EntryValue);
            _liteDb.GetCollection<CommandData>("CommandData").EnsureIndex(expression => expression.DeviceSerialNumber);
            _liteDb.GetCollection<CommandData>("CommandData").EnsureIndex(expression => expression.DeviceType);
            _liteDb.GetCollection<CommandData>("CommandData").EnsureIndex(expression => expression.LocalEntryTimestamp);

        }

        private void Upsert<T>(IEnumerable<T> data) where T : LumXData
        {
            try
            {
                var collectionName = typeof(T).ToString().Split('.').Last();
                _liteDb.GetCollection<T>(collectionName).Upsert(data);
            }
            catch (LiteException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        private IEnumerable<T> GetData<T>(string deviceSerialNumber, string deviceType) where T : LumXData
        {
            var sessionId = _httpContextAccessor.HttpContext.Session.Id;
            var collectionName = typeof(T).ToString().Split('.').Last();

            if (string.IsNullOrEmpty(deviceSerialNumber) && string.IsNullOrEmpty(deviceType))
            {
                return _liteDb.GetCollection<T>().Find(e => e.LocalSessionId == sessionId);
            }
            else if (string.IsNullOrEmpty(deviceType))
            {
                return _liteDb.GetCollection<T>(collectionName)
                                .Find(e => e.LocalSessionId == sessionId && e.DeviceSerialNumber == deviceSerialNumber);
            }
            else if (string.IsNullOrEmpty(deviceSerialNumber))
            {
                return _liteDb.GetCollection<T>(collectionName)
                                    .Find(e => e.LocalSessionId == sessionId && e.DeviceType == deviceType);
            }
            else
            {
                return _liteDb.GetCollection<T>(collectionName)
                                .Find(e => e.LocalSessionId == sessionId && e.DeviceSerialNumber == deviceSerialNumber && e.DeviceType == deviceType);
            }
        }

        private IQueryable<T> GetQueryable<T>() where T : LumXData
        {
            var sessionId = _httpContextAccessor.HttpContext.Session.Id;
            var collectionName = typeof(T).ToString().Split('.').Last();

            var query = _liteDb.GetCollection<T>(collectionName).Find(e => e.LocalSessionId == sessionId).AsQueryable();

            return query;
        }

        public IEnumerable<EventData> GetEventsData(string deviceSerialNumber, string deviceType)
        {
            return GetData<EventData>(deviceSerialNumber, deviceType);            
        }

        public IQueryable<EventData> GetEventsQueryable()
        {
            return GetQueryable<EventData>();
        }

        public int DeleteExpiredSessions()
        {
            var sessionLength = int.Parse(Configuration["LumX:SessionIdleTimeout"]);
            var timeExp = DateTime.UtcNow.AddMinutes(-sessionLength);

            var expiredSessions = _liteDb.GetCollection<Session>("Session").Find(s => s.StartTime < timeExp);

            foreach (var expiredSession in expiredSessions)
            {                
                _liteDb.GetCollection<EventData>("EventData").DeleteMany(e => e.LocalSessionId == expiredSession.SessionId);                
            }

            var expired = _liteDb.GetCollection<Session>("Session").DeleteMany(s => s.StartTime < timeExp);
            if (expired > 0)
            {
                _liteDb.Rebuild();
            }

            return expired;
        }

        public void UpsertEventsData(IEnumerable<EventData> eventsData)
        {
            Upsert(eventsData);
        }

        public string InsertSession(Session session)
        {
            var id = _liteDb.GetCollection<Session>("Session").Insert(session);
            return id.AsObjectId.ToString();
        }

        public Session GetSession(string id)
        {
            var session = _liteDb.GetCollection<Session>("Session").FindOne(s => s.Id == new ObjectId(id));

            return session;
        }

        public bool UpdateSession(string id, Session session)
        {
            Console.WriteLine($"UpdateSession {id}");
            return _liteDb.GetCollection<Session>("Session").Update(new ObjectId(id), session);
        }

        public bool UpdateSessionTime(string id)
        {
            var session = GetSession(id);

            if (session == null) 
            {
                Console.WriteLine($"Session for id {id} is null");
                return false;
            }            
            else
            {
                session.StartTime = DateTime.UtcNow;
                return UpdateSession(id, session);
            }
        }

        public void Clear<T>(string sessionId) where T : LumXData
        {
            var collectionName = typeof(T).ToString().Split('.').Last();
            _liteDb.GetCollection<T>(collectionName).DeleteMany(e => e.LocalSessionId == sessionId);
        }

        public string GetIdForUser(User sessionUser)
        {
            var sessions = _liteDb.GetCollection<Session>("Session").FindAll().ToList();
            Session found = null;

            foreach (var session in sessions)
            {
                if (session.User.Equals(sessionUser))
                {
                    found = session;
                    break;
                }
            }

            return found?.Id.ToString();
        }

        public bool SessionIsExpired(string sessionId)
        {
            var sessionLength = int.Parse(Configuration["LumX:SessionIdleTimeout"]);
            var timeExp = DateTime.UtcNow.AddMinutes(-sessionLength);

            var session = _liteDb.GetCollection<Session>("Session").FindOne(s => s.SessionId == sessionId);

            if (session == null)
                return false;

            if (session.StartTime < timeExp)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<CommandData> GetCommandsData(string deviceSerialNumber, string deviceType)
        {
            return GetData<CommandData>(deviceSerialNumber, deviceType);
        }

        public void UpsertCommandsData(IEnumerable<CommandData> commandsData)
        {
            Upsert(commandsData);
        }

        public IQueryable<CommandData> GetCommandsQueryable()
        {
            return GetQueryable<CommandData>();
        }
    }
}
