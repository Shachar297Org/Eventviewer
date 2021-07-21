using EventViewer.Interfaces;
using EventViewer.Models;
using EventViewer.Models.ApiLogin;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventViewer.Services
{
    public class UserApiAuthenticationService : IUserApiAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration Configuration;

        public UserApiAuthenticationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            Configuration = configuration;
        }

        public async Task<User> GetTokensForUser(User user, string tokenUri)
        {
            HttpResponseMessage response = null;

            if (user.UsesBasicAuth)
            {
                var body = new { email = Configuration["LumX:Username"], password = Configuration["LumX:Password"] };
                response = await _httpClient.PostAsync(tokenUri, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            }
            else
            {
                //var body = new { entityId = user.EntityId, operation = user.Operation };
                //response = await _httpClient.PostAsync(tokenUri, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            }

            if (!response.IsSuccessStatusCode || user == null)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            ApiLoginResponse responseObject = JsonSerializer.Deserialize<ApiLoginResponse>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var accessToken = responseObject.AccessJwt;
            var refreshToken = responseObject.RefreshJwt;

            user.AccessToken = accessToken;
            user.RefreshToken = refreshToken;

            return user;
        }

        public async Task<User> RefreshTokensForUser(User user, string tokenUri)
        {
            var body = new { refreshToken = user.RefreshToken.Token };
            var response = await _httpClient.PostAsync(tokenUri, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                AuthOperationData responseObject = JsonSerializer.Deserialize<AuthOperationData>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var accessToken = responseObject.AccessJwt;
                var refreshToken = responseObject.RefreshJwt;

                user.AccessToken = accessToken;
                user.RefreshToken = refreshToken;

                return user;
            }

            return null;
        }
    }
}
