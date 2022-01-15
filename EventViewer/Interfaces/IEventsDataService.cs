using EventViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewer.Interfaces
{
    public interface IEventsDataService
    {
        Task<List<EventData>> GetEventsData(DateTime? from = null, DateTime? to = null, Device device = null, string userId = null);
        Task<bool> CheckCredentials(string environment, string userId);
    }
}
