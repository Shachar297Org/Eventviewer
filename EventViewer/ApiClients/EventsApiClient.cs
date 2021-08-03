using EventViewer.Exceptions;
using EventViewer.Interfaces;
using EventViewer.Models;
using EventViewer.Models.ApiLogin;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EventViewer.ApiClients
{
    public class EventsApiClient : IEventsApiClient
    {
        private readonly HttpClient _httpClient;
        private static string _eventsApiUrl = "/processing/v1/events";

        private string ApiBuildUrl(string ga, string sn, DateTime? dtFrom, DateTime? dtTo)
        {
            string paramNameDeviceSn = "deviceSerialNumber";
            string paramNameDeviceType = "deviceType";
            string paramNameFltDateFrom = "from";
            string paramNameFltDateTo = "to";

            string paramValueDeviceSn = (sn ?? "").Trim();
            string paramValueDeviceType = (ga ?? "").Trim();
            string dtFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            string paramValueFltDateFrom = dtFrom?.ToString(dtFormat) ?? "";
            string paramValueFltDateTo = dtTo?.ToString(dtFormat) ?? "";

            var query = new NameValueCollection(); 
            if (!string.IsNullOrWhiteSpace(paramValueDeviceSn))
                query[paramNameDeviceSn] = paramValueDeviceSn;
            if (!string.IsNullOrWhiteSpace(paramValueDeviceType))
                query[paramNameDeviceType] = paramValueDeviceType;
            if (dtFrom.HasValue)
                query[paramNameFltDateFrom] = paramValueFltDateFrom;
            else
                query[paramNameFltDateFrom] = DateTime.MinValue.ToString(dtFormat);
            if (dtTo.HasValue)
                query[paramNameFltDateTo] = paramValueFltDateTo;
            else
                query[paramNameFltDateTo] = DateTime.UtcNow.ToString(dtFormat);

            string q = String.Join("&", query.AllKeys.Select(a => HttpUtility.UrlEncode(a) + "=" + HttpUtility.UrlEncode(query[a])));

            string url = _eventsApiUrl + "?" + q;
            return url;
        }

        public EventsApiClient(
            HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        private async Task<List<EventData>> GetEventsViaApi(string apiUrl, string environment, CancellationToken cancellationToken)
        {
            _httpClient.BaseAddress = new Uri($"https://api.{environment}.lumenisx.lumenis.com");
            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return new List<EventData>();
            }

            if (!response.IsSuccessStatusCode)
            {
                
                string errorJson = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.BadGateway || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    throw new EventViewerException(EventViewerError.NOT_SUCCEEDED, "Something wrong happened. Couldn't retrieve the events for your query.");
                }

                var errorResponseObject = JsonSerializer.Deserialize<ApiErrorResponse>(errorJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (errorResponseObject?.Code == null)
                {
                    throw new EventViewerException(EventViewerError.NOT_FOUND, "Not found any events for this query");
                }
                else if (errorResponseObject?.Code == "LIMIT_EXCEEDED")
                {
                    throw new EventViewerException(EventViewerError.LIMIT_EXCEEDED, "The amout of events for this query reached the API limit");
                }
                else if (errorResponseObject?.Code == "ACCESS_DENIED" || errorResponseObject?.Code == "TOKEN_NOT_VALID")
                {
                    throw new EventViewerException(EventViewerError.INVALID_CREDENTIALS, "Access denied. Try to get new credentials through the Portal.");
                }
                
                return new List<EventData>();
            }

            string json = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ApiRefreshResponse>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (responseObject != null)
            {
                return responseObject.Data;
            }
            else
            {
                return new List<EventData>();
            }
        }

        public async Task<List<EventData>> GetEvents(string environment, DateTime? from = null, DateTime? to = null, Device device = null, CancellationToken cancellationToken = default)
        {
            var sn = device?.DeviceSerialNumber;
            var ga = device?.DeviceType;

            var apiUrl = ApiBuildUrl(ga, sn, from, to);

            return await GetEventsViaApi(apiUrl, environment, cancellationToken); 
        }
    }
}
