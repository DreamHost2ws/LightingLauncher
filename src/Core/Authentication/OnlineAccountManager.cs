using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AgLauncher.Core.Authentication
{
    /// <summary>
    /// Manages online Minecraft accounts (Microsoft/Mojang authentication)
    /// </summary>
    public class OnlineAccountManager
    {
        private const string MICROSOFT_AUTH_URL = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
        private const string XBOX_AUTH_URL = "https://user.auth.xboxlive.com/user/authenticate";
        private const string MINECRAFT_AUTH_URL = "https://api.minecraftservices.com/authentication/login_with_xbox";

        private readonly HttpClient _httpClient;
        private readonly string _clientId = "00000000402b5328"; // Minecraft client ID

        public OnlineAccountManager()
        {
            _httpClient = new HttpClient();
        }

        public class MicrosoftToken
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }
        }

        public class MinecraftProfile
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string SkinUrl { get; set; }
            public string CapeUrl { get; set; }
        }

        /// <summary>
        /// Authenticate with Microsoft account
        /// </summary>
        public async Task<MicrosoftToken> AuthenticateMicrosoftAsync(string code)
        {
            try
            {
                var request = new Dictionary<string, string>
                {
                    { "client_id", _clientId },
                    { "code", code },
                    { "grant_type", "authorization_code" }
                };

                var content = new FormUrlEncodedContent(request);
                var response = await _httpClient.PostAsync(
                    "https://login.microsoftonline.com/consumers/oauth2/v2.0/token", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<MicrosoftToken>(json);
                }

                throw new Exception("Microsoft authentication failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"Microsoft auth error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get Minecraft profile from Xbox token
        /// </summary>
        public async Task<MinecraftProfile> GetMinecraftProfileAsync(string xboxToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://api.minecraftservices.com/minecraft/profile");
                request.Headers.Add("Authorization", $"Bearer {xboxToken}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<MinecraftProfile>(json);
                }

                throw new Exception("Failed to retrieve Minecraft profile");
            }
            catch (Exception ex)
            {
                throw new Exception($"Profile retrieval error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate access token
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://api.minecraftservices.com/minecraft/profile");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
