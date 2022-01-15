using EventViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewer.Interfaces
{
    public interface IEventsApiClient
    {
        Task<List<EventData>> GetEvents(string environment, string userid, DateTime? from = null, DateTime? to = null, Device? device = null, CancellationToken cancellationToken = default);
    }
}
