using EventViewer.ApiClients;
using EventViewer.Exceptions;
using EventViewer.Interfaces;
using EventViewer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewer.Services
{
    public class EventsDataService : IEventsDataService
    {
        private readonly IEventsApiClient _client;
        private readonly ILiteDBEventsDataService _liteDBService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IConfiguration Configuration;

        public EventsDataService(IEventsApiClient client, ILiteDBEventsDataService liteDBService, IHttpContextAccessor httpContextAccessor,
                                 IConfiguration configuration)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _liteDBService = liteDBService ?? throw new ArgumentNullException(nameof(liteDBService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<EventData>> GetEventsData(DateTime? from = null, DateTime? to = null, Device device = null)
        {
            CancellationToken cancellationToken = _httpContextAccessor.HttpContext.RequestAborted;
            if (cancellationToken.IsCancellationRequested)
            {
                return new List<EventData>();
            }

            var result = await _client.GetEvents(from, to, device, cancellationToken);
            
            var localSession = _httpContextAccessor.HttpContext.Session.Id;

            foreach (var item in result)
            {
                item.LocalSessionId = localSession;                
            }

            await Task.Run(() =>
            {

                if (!cancellationToken.IsCancellationRequested)
                {
                    _liteDBService.Clear(localSession);
                    _liteDBService.UpsertEventsData(result);
                }

            }, cancellationToken);
            
            return result;
        }

        public async Task<bool> CheckCredentials(User user)
        {
            var from = DateTime.MinValue;
            var to = DateTime.UtcNow;

            var device = new Device
            {
                DeviceSerialNumber = Configuration["LumX:TestDeviceSN"],
                DeviceType = Configuration["LumX:TestDeviceType"]
            };

            try
            {
                await _client.GetEvents(from, to, device);
            }
            catch(EventViewerException ex)
            {
                if (ex.ErrorCode == EventViewerError.INVALID_CREDENTIALS)
                {
                    return false;
                }
                
            }

            return true;
            
        }

    }
}
