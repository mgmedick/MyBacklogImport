using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Linq;
using GameStatsAppImport.Interfaces.Services;
using GameStatsAppImport.Model.JSON;

namespace GameStatsApp.Service
{
    public class AuthService : IAuthService
    {
        private readonly ISettingService _settingService = null; 
        private readonly IConfiguration _config = null;

        public AuthService(ISettingService settingService, IConfiguration config)
        {
            _settingService = settingService;
            _config = config;
        }

        #region TwitchAuth
        public async Task<string> GetTwitchAccessToken()
        {
            var tokenString = _settingService.GetSetting("TwitchAcccessToken")?.Str;
            var expireDate = _settingService.GetSetting("TwitchAcccessTokenExpireDate")?.Dte;

            if (string.IsNullOrWhiteSpace(tokenString) || expireDate < DateTime.UtcNow)
            {
                var token = await AuthenticateTwitch();
                _settingService.UpdateSetting("TwitchAcccessToken", token.access_token);
                _settingService.UpdateSetting("TwitchAcccessTokenExpireDate", DateTime.UtcNow.AddSeconds(token.expires_in));
                tokenString = token.access_token; 
            }

            return tokenString;
        }

        public async Task<Token> AuthenticateTwitch()
        {
            Token data = null;
            var clientID = _config.GetSection("Auth").GetSection("Twitch").GetSection("ClientId").Value;
            var clientSecret = _config.GetSection("Auth").GetSection("Twitch").GetSection("ClientSecret").Value;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var parameters = new Dictionary<string,string>{
                    {"client_id", clientID},
                    {"grant_type", "client_credentials"},
                    {"client_secret", clientSecret}
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token") { Content = new FormUrlEncodedContent(parameters) };

                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var dataString = await response.Content.ReadAsStringAsync();
                        data = JsonConvert.DeserializeObject<Token>(dataString);
                    }
                }
            }

            return data;
        }    
        #endregion
    }
}
