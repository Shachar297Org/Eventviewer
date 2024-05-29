﻿using EventViewer.ApiClients;
using EventViewer.Exceptions;
using EventViewer.Interfaces;
using EventViewer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewer.Services
{
    public class EventsDataService : IEventsDataService
    {
        private readonly IEventsApiClient _eventsApiClient;
        private readonly ICommandsApiClient _commandsApiClient;

        private readonly ILiteDBDataService _liteDBService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IConfiguration Configuration;

        public EventsDataService(IEventsApiClient eventsClient, ICommandsApiClient commandsClient, ILiteDBDataService liteDBService, IHttpContextAccessor httpContextAccessor,
                                 IConfiguration configuration)
        {
            _eventsApiClient = eventsClient ?? throw new ArgumentNullException(nameof(eventsClient));
            _commandsApiClient = commandsClient ?? throw new ArgumentNullException(nameof(commandsClient));

            _liteDBService = liteDBService ?? throw new ArgumentNullException(nameof(liteDBService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<EventData>> GetEventsData(DateTime? from = null, DateTime? to = null, Device device = null, string userId = null)
        {
            var env = _httpContextAccessor.HttpContext.Request.Host.Host.Split('.');
            var environment = env[0] == "localhost" ? "int" : env[1];

            CancellationToken cancellationToken = _httpContextAccessor.HttpContext.RequestAborted;
            if (cancellationToken.IsCancellationRequested)
            {
                return new List<EventData>();
            }

            var localSession = _httpContextAccessor.HttpContext.Session.Id;

            var result = await _eventsApiClient.GetEvents(environment, userId, from, to, device, cancellationToken);           

            foreach (var item in result)
            {
                item.LocalSessionId = localSession;                
            }

            await Task.Run(() =>
            {

                if (!cancellationToken.IsCancellationRequested)
                {
                    _liteDBService.Clear<EventData>(localSession);
                    _liteDBService.UpsertEventsData(result);
                }

            }, cancellationToken);
            
            return result;
        }

        public async Task<bool> CheckCredentials(string environment, string userId)
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
                await _eventsApiClient.GetEvents(environment, userId, from, to, device);
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

        public async Task<List<CommandData>> GetCommandsData(DateTime? from = null, DateTime? to = null, Device device = null, string userId = null)
        {
            var env = _httpContextAccessor.HttpContext.Request.Host.Host.Split('.');
            var environment = env[0] == "localhost" ? "int" : env[1];

            CancellationToken cancellationToken = _httpContextAccessor.HttpContext.RequestAborted;
            if (cancellationToken.IsCancellationRequested)
            {
                return new List<CommandData>();
            }

            var localSession = _httpContextAccessor.HttpContext.Session.Id;

            var result = await _commandsApiClient.GetCommands(environment, userId, from, to, device, cancellationToken);
            var commands = result.Where(c => c.DeviceSerialNumber == device.DeviceSerialNumber && c.DeviceType == device.DeviceType).ToList();   

            foreach (var item in commands)
            {
                item.LocalSessionId = localSession;
            }

            await Task.Run(() =>
            {

                if (!cancellationToken.IsCancellationRequested)
                {
                    _liteDBService.Clear<CommandData>(localSession);
                    _liteDBService.UpsertCommandsData(commands);
                }

            }, cancellationToken);

            return result;
        }
    }
}
