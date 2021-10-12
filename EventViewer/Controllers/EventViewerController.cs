﻿using EventViewer.Interfaces;
using EventViewer.Models;
using EventViewer.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EventViewer.Extensions;
using System.Text.Json;
using JqueryDataTables.ServerSide.AspNetCoreWeb.Models;
using EventViewer.Models.ApiLogin;
using System.Globalization;
using EventViewer.Exceptions;
using Microsoft.Extensions.Configuration;
using System.Threading;
using EventViewer.Middleware;

namespace EventViewer.Controllers
{
    public class EventViewerController : Controller
    {
        private readonly ILogger<EventViewerController> _logger;
        private readonly IEventsDataService _eventsDataService;
        private readonly ILiteDBEventsDataService _liteDbService;
        private readonly IUserApiAuthenticationService _authenticationService;

        public EventViewerController(ILogger<EventViewerController> logger, IEventsDataService eventsService, ILiteDBEventsDataService liteDbService,
                                     IUserApiAuthenticationService authenticationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventsDataService = eventsService ?? throw new ArgumentNullException(nameof(eventsService));
            _liteDbService = liteDbService ?? throw new ArgumentNullException(nameof(liteDbService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

        }

        [Route("/error")]
        public IActionResult Error(string errorMessage, EventViewerError errorCode)
        {
            return View(errorMessage);
        }

        [HttpPost("~/cspreport")]
        public IActionResult CspReport([FromBody] CspReportRequest request)
        {
            // TODO: log request to a datastore somewhere
            _logger.LogWarning($"CSP Violation: {request.CspReport.DocumentUri}, {request.CspReport.BlockedUri}");

            return Ok();
        }


        [Route("/{id?}")]
        public IActionResult Index(string id)
        {           
            if (id == null)
            {
                return new ObjectResult("Access denied") { StatusCode = 403 };
            }

            Session session = null;
            if (id != null)
            {
                session = _liteDbService.GetSession(id);
            }

            if (session == null)
            {
                return new ObjectResult("Access denied") { StatusCode = 403 };
            }
            else
            {
                               
                var sessionIsExpired = _liteDbService.SessionIsExpired(HttpContext.Session.Id);
                if (sessionIsExpired || session.SessionId != HttpContext.Session.Id)
                {
                    _logger.LogWarning($"Expired or mismatch: {sessionIsExpired}, {session.SessionId != HttpContext.Session.Id}"); 
                    return new ObjectResult("Access denied") { StatusCode = 403 };
                }                

                session.StartTime = DateTime.UtcNow;
                _liteDbService.UpdateSession(id, session);

                HttpContext.Session.Set<User>("user", session.User);
            }

            return View();
        }
 

        [HttpPost]
        [Route("/setup")]
        public async Task<IActionResult> Setup(string accessToken, long accessTokenExpiration, string refreshToken, long refreshTokenExpiration)
        {
            var env = HttpContext.Request.Host.Host.Split('.');
            var environment = env[0] == "localhost" ? "int" : env[1];
            _logger.LogInformation($"Environment: {environment}");

            if (accessToken == null || refreshToken == null || string.IsNullOrWhiteSpace(environment))
            {
                return new ObjectResult("Access denied") { StatusCode = 403 };
            }

            var sessionUser = HttpContext.Session.Get<User>("user");
            var id = string.Empty;

            if (sessionUser != null && environment == sessionUser.Environment)
            {
                _logger.LogInformation($"Getting existing user");
                id = _liteDbService.GetIdForUser(sessionUser);
            }
            else
            {
                User user = new User()
                {
                    AccessToken = new JwtToken { Token = accessToken, Expiration = accessTokenExpiration },
                    RefreshToken = new JwtToken { Token = refreshToken, Expiration = refreshTokenExpiration },
                    Environment = environment
                };
                HttpContext.Session.Set<User>("user", user);

                var session = new Session
                {
                    User = user,
                    SessionId = HttpContext.Session.Id,
                    StartTime = DateTime.UtcNow
                };

                var goodToGo = await _eventsDataService.CheckCredentials(environment);
                if (!goodToGo)
                {
                    _logger.LogError($"Credentials are not good");
                    return new ObjectResult("Access denied") { StatusCode = 403 };
                }

                id = _liteDbService.InsertSession(session);

                if (id == null)
                {
                    _logger.LogError($"Could not insert new session");
                    return new ObjectResult("Access denied") { StatusCode = 403 };
                }

                _logger.LogInformation($"Setup Session: {HttpContext.Session.Id}");
            }


            return RedirectToAction("Index", new { id = id });
        }

        /*
        
        [HttpPost]
        [Route("/setupApi")]
        public async Task<IActionResult> SetupApi([FromBody] ApiLoginResponse tokens)
        {
            var env = HttpContext.Request.Host.Host.Split('.');
            var environment = env[0] == "localhost" ? "int" : env[1];

            if (tokens == null || tokens.AccessJwt == null || tokens.RefreshJwt == null)
            {
                return new ObjectResult("Access denied") { StatusCode = 403 }; 
            }

            var sessionUser = HttpContext.Session.Get<User>("user");
            var id = string.Empty;

            if (sessionUser != null)
            {
                id = _liteDbService.GetIdForUser(sessionUser);
            }
            else
            {
                User user = new User()
                {
                    AccessToken = tokens.AccessJwt,
                    RefreshToken = tokens.RefreshJwt
                };
                HttpContext.Session.Set<User>("user", user);

                var session = new Session
                {
                    User = user
                };

                var goodToGo = await _eventsDataService.CheckCredentials(environment);
                if (!goodToGo)
                {
                    return new ObjectResult("Access denied") { StatusCode = 403 };
                }

                id = _liteDbService.InsertSession(session);
            }

            return Json(new { id = id }); // (new { id = id });
        }
        */
        [HttpGet]
        [Route("/setupBasic")]
        public async Task<IActionResult> SetupBasic()
        {
            var env = HttpContext.Request.Host.Host.Split('.');
            
            var environment = env[0] == "localhost" ? "int" : env[1];

            if (environment != "int")
                return new ObjectResult("Access denied") { StatusCode = 403 };

            var sessionUser = HttpContext.Session.Get<User>("user");
            
            var id = string.Empty;

            if (sessionUser != null && environment == sessionUser.Environment)
            {
                id = _liteDbService.GetIdForUser(sessionUser);
            }
            else
            {
                sessionUser = new User
                {
                    UsesBasicAuth = true,
                    Environment = environment
                };

                var tokenUri = $"https://api.{environment}.lumenisx.lumenis.com/ums/v1/users/loginCredentials";
                var user = await _authenticationService.GetTokensForUser(sessionUser, tokenUri);

                if (user != null)
                {
                    HttpContext.Session.Set<User>("user", user);

                    var session = new Session
                    {
                        User = user,
                        SessionId = HttpContext.Session.Id,
                        StartTime = DateTime.UtcNow
                    };

                    id = _liteDbService.InsertSession(session);

                    Console.WriteLine($"Setup BasicAuth Session: {HttpContext.Session.Id}");
                }
                else
                {
                    throw new EventViewerException(EventViewerError.INVALID_CREDENTIALS, "Something wrong with your basic auth credentials");
                }
                
            }

            return RedirectToAction("Index", new { id = id });
        }

        [HttpPost]
        [Route("/search")]
        public async Task<IActionResult> Search(string id, string from, string to, string deviceSerialNumber, string deviceType)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false });
            }
            try
            {
                CancellationToken cancellationToken = HttpContext.RequestAborted;
                if (cancellationToken.IsCancellationRequested)
                {
                    return Json(new { success = true });
                }

                Device device = null;

                if (!string.IsNullOrEmpty(deviceSerialNumber) && !string.IsNullOrEmpty(deviceType))
                {
                    device = new Device
                    {
                        DeviceSerialNumber = deviceSerialNumber,
                        DeviceType = deviceType
                    };
                }
                else if (string.IsNullOrWhiteSpace(deviceSerialNumber) || string.IsNullOrWhiteSpace(deviceType))
                {
                    return Json(new { success = false, errorMessage = "Please fill in both Device Type and Device Serial Number fields with non empty values" });
                }

                CultureInfo provider = CultureInfo.InvariantCulture;
                var fromDate = DateTime.ParseExact(from, "yyyy/MM/dd HH:mm", provider);
                var toDate = DateTime.ParseExact(to, "yyyy/MM/dd HH:mm", provider);

                var sessionIsExpired = _liteDbService.SessionIsExpired(HttpContext.Session.Id);
                if (sessionIsExpired)
                    return Json(new { success = false, errorMessage = "Session is expired. Please login again through the Portal" });

                bool success = _liteDbService.UpdateSessionTime(id);
                
                if (!success)
                {
                    _logger.LogInformation("Could not update time");
                }                                   

                await _eventsDataService.GetEventsData(fromDate, toDate, device);
            }
            catch (EventViewerException ex)
            {
                return Json(new { success = false, errorCode = ex.ErrorCode, errorMessage = ex.Message });
            }
            catch (TaskCanceledException ce)
            {
                _logger.LogError(ce.Message);
                _logger.LogError(ce.StackTrace);

                return Json(new { success = false, errorMessage = "Operation canceled. Processing of your request takes too much time." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);

                return Json(new { success = false, errorMessage = "Something wrong happened. Couldn't retrieve the events for your query." });
            }
        
            return Json(new { success = true });
        }

        [HttpPost]
        [Route("/getEvents")]
        public IActionResult GetEvents()
        {
            try
            {
                //var sessionIsExpired = _liteDbService.SessionIsExpired(HttpContext.Session.Id);
                //if (sessionIsExpired)
                //    return BadRequest();

                var id = Request.Form["id"].FirstOrDefault();
                //bool success = _liteDbService.UpdateSessionTime(id);
                //if (!success)
                //    return BadRequest();

                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                var eventsData = _liteDbService.GetEventsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    var direction = sortColumnDirection == "asc" ? 1 : 0;
                    //eventsData = eventsData.OrderBy(sortColumn, direction);

                    System.Reflection.PropertyInfo prop = typeof(EventData).GetProperty(sortColumn);

                    eventsData = sortColumnDirection == "asc" ? eventsData.OrderBy(c => prop.GetValue(c, null)) : eventsData.OrderByDescending(c => prop.GetValue(c, null));
                }
                if (!string.IsNullOrEmpty(searchValue))
                {
                    eventsData = eventsData.Where(m => m.DeviceSerialNumber.Contains(searchValue)
                                                || m.DeviceType.Contains(searchValue)
                                                || m.EntryKey.Contains(searchValue)
                                                || m.EntryValue.Contains(searchValue)
                                                || m.EntryTimestamp.Contains(searchValue));
                }
                recordsTotal = eventsData.Count();
                //var data = eventsData.Skip(skip).Limit(pageSize).ToList();
                var data = eventsData.Skip(skip).Take(pageSize).ToList();
                var jsonData = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data };

                return Ok(jsonData);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
