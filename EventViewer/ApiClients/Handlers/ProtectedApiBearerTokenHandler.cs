using EventViewer.Interfaces;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EventViewer.Extensions;
using EventViewer.Models;
using EventViewer.Exceptions;

namespace EventViewer.ApiClients
{
    public class ProtectedApiBearerTokenHandler : DelegatingHandler
    {
        private readonly IUserApiAuthenticationService _authenticationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IConfiguration Configuration;

        private readonly static string _basicAuthTokenUrl = "/ums/v1/users/loginCredentials";
        private readonly static string _refreshTokenUrl = "/ums/v1/users/current/refreshToken";
        //private readonly static string _tokenAuthTokenUrl = "/ums/v1/tokens/{0}?entityId={1}&operation={2}";

        public ProtectedApiBearerTokenHandler(IUserApiAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _authenticationService = authenticationService;
            _httpContextAccessor = httpContextAccessor;

            Configuration = configuration;
    }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {            
            var baseUri = request.RequestUri.GetLeftPart(UriPartial.Authority);
            var isBasicAuth = Configuration["LumX:Authentication"] == "basic";

            User user = _httpContextAccessor.HttpContext.Session.Get<User>("user");
            Console.WriteLine($"API Session {_httpContextAccessor.HttpContext.Session.Id} user: {user?.AccessToken} ");

            if (!isBasicAuth && (user == null || user.AccessToken == null))
            {
                throw new EventViewerException(EventViewerError.INVALID_CREDENTIALS, "Access denied, please log in again through Portal");
            }
            else
            {
                if (isBasicAuth && (user == null || user.AccessToken == null))
                {
                    var tokenUri = baseUri + _basicAuthTokenUrl;

                    user = await _authenticationService.GetTokensForUser(user, tokenUri, isBasicAuth:true);

                    if (user != null)
                    {
                        _httpContextAccessor.HttpContext.Session.Set<User>("user", user);
                    }
                    else
                    {
                        throw new EventViewerException(EventViewerError.INVALID_CREDENTIALS, "Something wrong with your basic auth credentials");
                    }

                }
                else
                {
                    DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var expirationDate = start.AddMilliseconds(user.AccessToken.Expiration).ToLocalTime();

                    if (expirationDate <= DateTime.Now)
                    {
                        // Refresh

                        var tokenUri = baseUri + _refreshTokenUrl;

                        user = await _authenticationService.RefreshTokensForUser(user, tokenUri, isBasicAuth);

                        if (user != null)
                        {
                            _httpContextAccessor.HttpContext.Session.Set<User>("user", user);
                        }
                        else
                        {
                            throw new EventViewerException(EventViewerError.INVALID_CREDENTIALS, "Couldn't get refreshed tokens");
                        }

                    }
                    
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken.Token);
                return await base.SendAsync(request, cancellationToken);
            }

        }
    }
}
