using EventViewer.Interfaces;
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

namespace EventViewer.Controllers
{
    public class EventViewerController : Controller
    {
        private readonly ILogger<EventViewerController> _logger;
        private readonly IEventsDataService _eventsDataService;
        private readonly ILiteDBEventsDataService _liteDbService;

        private readonly IConfiguration Configuration;

        public EventViewerController(ILogger<EventViewerController> logger, IEventsDataService eventsService, ILiteDBEventsDataService liteDbService,
                                     IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventsDataService = eventsService ?? throw new ArgumentNullException(nameof(eventsService));
            _liteDbService = liteDbService ?? throw new ArgumentNullException(nameof(liteDbService));

            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [Route("/error")]
        public IActionResult Error(string errorMessage, EventViewerError errorCode)
        {
            return View(errorMessage);
        }


        [Route("/{id?}")]
        public IActionResult Index(string id)
        {           
            if (id == null)
            {
                return Ok();
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
                //Console.WriteLine($"Index Session: {HttpContext.Session.Id}");

                session.SessionId = HttpContext.Session.Id;
                session.StartTime = DateTime.UtcNow;

                _liteDbService.UpdateSession(id, session);

                HttpContext.Session.Set<User>("user", session.User);
            }

            return View();
        }

        [HttpPost]
        [Route("/setupApi")]
        public async Task<IActionResult> SetupApi([FromBody] ApiLoginResponse tokens)
        {           
            if (tokens == null || tokens.AccessJwt == null || tokens.RefreshJwt == null)
            {
                return new ObjectResult("Access denied") { StatusCode = 403 }; // return BadRequest();
            }

            User user = new User()
            {
                AccessToken = tokens.AccessJwt,
                RefreshToken = tokens.RefreshJwt
            };
            HttpContext.Session.Set<User>("user", user);

            var goodToGo = await _eventsDataService.CheckCredentials(user);
            if (!goodToGo)
            {
                return BadRequest();
            }

            var session = new Session
            {
                User = user
            };

            var id =_liteDbService.InsertSession(session);

            return Json(new { id = id }); // (new { id = id });
        }

        [HttpPost]
        [Route("/setup")]
        public async Task<IActionResult> Setup(string accessToken, long accessTokenExpiration, string refreshToken, long refreshTokenExpiration)
        {
            if (accessToken == null || refreshToken == null)
            {
                return new ObjectResult("Access denied") { StatusCode = 403 }; //return BadRequest();
            }

            User user = new User()
            {
                AccessToken = new JwtToken { Token = accessToken, Expiration = accessTokenExpiration },
                RefreshToken = new JwtToken { Token = refreshToken, Expiration = refreshTokenExpiration }
            };
            HttpContext.Session.Set<User>("user", user);

            var goodToGo = await _eventsDataService.CheckCredentials(user);
            if (!goodToGo)
            {
                return BadRequest();
            }

            var session = new Session
            {
                User = user
            };

            var id = _liteDbService.InsertSession(session);

            Console.WriteLine($"Setup Session: {HttpContext.Session.Id}");

            return RedirectToAction("Index", new { id = id });
        }

        [HttpGet]
        [Route("/setupBasic")]
        public async Task<IActionResult> SetupBasic()
        {
            var user = new User();

            var session = new Session
            {
                User = user
            };

            var id = _liteDbService.InsertSession(session);

            return RedirectToAction("Index", new { id = id });
        }

        [HttpPost]
        [Route("/search")]
        public async Task<IActionResult> Search(string id, string from, string to, string deviceSerialNumber, string deviceType)
        {
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

                CultureInfo provider = CultureInfo.InvariantCulture;
                var fromDate = DateTime.ParseExact(from, "yyyy/MM/dd HH:mm", provider);
                var toDate = DateTime.ParseExact(to, "yyyy/MM/dd HH:mm", provider);

                _liteDbService.UpdateSessionTime(id);

                await _eventsDataService.GetEventsData(fromDate, toDate, device);
            }
            catch (EventViewerException ex)
            {
                if (ex.ErrorCode == EventViewerError.INVALID_CREDENTIALS)
                {
                    return Json(new { success = false, errorCode = ex.ErrorCode, errorMessage = ex.Message });
                }
                else if (ex.ErrorCode == EventViewerError.LIMIT_EXCEEDED)
                {
                    return Json(new { success = false, errorCode = ex.ErrorCode, errorMessage = ex.Message });
                }
                else if (ex.ErrorCode == EventViewerError.NOT_FOUND)
                {
                    return Json(new { success = false, errorCode = ex.ErrorCode, errorMessage = ex.Message });
                }
                
            }
            catch (TaskCanceledException ce)
            {
                return Json(new { success = false });
            }
        
            return Json(new { success = true });
        }

        [HttpPost]
        [Route("/getEvents")]
        public IActionResult GetEvents()
        {
            try
            {
                var id = Request.Form["id"].FirstOrDefault();
                _liteDbService.UpdateSessionTime(id);

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
