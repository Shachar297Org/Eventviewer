﻿using DocumentFormat.OpenXml.Spreadsheet;
using EventViewer.Exceptions;
using EventViewer.Interfaces;
using EventViewer.Models;
using EventViewer.Models.ApiLogin;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EventViewer.ApiClients
{
    public class CommandsApiClient : ICommandsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CommandsApiClient> _logger;

        private static string _commandsApiUrl = "/processing/v1/commands";

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

            string url = _commandsApiUrl + "?" + q;
            return url;
        }

        public CommandsApiClient(
            HttpClient httpClient,
            ILogger<CommandsApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        private async Task<List<CommandData>> GetCommandsViaApi(string apiUrl, string environment, object userId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(string.Format("Environment: {0}, User: {1}, API call: {2}", environment, userId, apiUrl));

                _httpClient.BaseAddress = new Uri($"https://api.{environment}.lumenisx.lumenis.com");
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return new List<CommandData>();
                }

                if (!response.IsSuccessStatusCode)
                {

                    string errorJson = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.BadGateway || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        throw new EventViewerException(EventViewerError.NOT_SUCCEEDED, "Received an error from AWS service. Couldn't retrieve the commands for your query.");
                    }

                    var errorResponseObject = JsonSerializer.Deserialize<ApiErrorResponse>(errorJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (errorResponseObject?.Code == null)
                    {
                        throw new EventViewerException(EventViewerError.NOT_FOUND, "Not found any commands for this query");
                    }
                    else if (errorResponseObject?.Code == "LIMIT_EXCEEDED")
                    {
                        throw new EventViewerException(EventViewerError.LIMIT_EXCEEDED, "The amout of commands for this query reached the API limit of 500,000 per request");
                    }
                    else if (errorResponseObject?.Code == "ACCESS_DENIED" || errorResponseObject?.Code == "TOKEN_NOT_VALID")
                    {
                        throw new EventViewerException(EventViewerError.INVALID_CREDENTIALS, "Access denied. Try to get new credentials through the Portal.");
                    }

                    return new List<CommandData>();
                }

                string json = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<ApiRefreshResponse<CommandData>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (responseObject != null)
                {
                    return responseObject.Data;
                }
                else
                {
                    return new List<CommandData>();
                }
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "HttpRequestException when calling the API");
                throw;
            }
            catch (TimeoutException exception)
            {
                _logger.LogError(exception, "TimeoutException during call to API");
                throw;
            }
            catch (OutOfMemoryException exception)
            {
                _logger.LogError(exception, "Run out of memmory when processing the response from the API");
                throw new EventViewerException(EventViewerError.OUT_OF_MEMORY, "Too many commands received for current EventViewer configuration. Please use narrower date range or use QLIK to view the commands.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception when calling the API");
                throw;
            }
        }

        public async Task<List<CommandData>> GetCommands(string environment, string userId, DateTime? from = null, DateTime? to = null, Device device = null, CancellationToken cancellationToken = default)
        {
            var sn = device?.DeviceSerialNumber;
            var ga = device?.DeviceType;

            var apiUrl = ApiBuildUrl(ga, sn, from, to);

            return await GetCommandsViaApi(apiUrl, environment, userId, cancellationToken);
        }


    }
}
