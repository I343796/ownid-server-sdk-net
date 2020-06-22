using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using OwnIdSdk.NetCore3.Web.Gigya.Contracts;
using OwnIdSdk.NetCore3.Web.Gigya.Contracts.Login;
using OwnIdSdk.NetCore3.Web.Gigya.Contracts.UpdateProfile;

namespace OwnIdSdk.NetCore3.Web.Gigya
{
    public class GigyaRestApiClient
    {
        private readonly GigyaConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GigyaRestApiClient(GigyaConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<GetAccountInfoResponse> GetUserInfoByUid(string uid)
        {
            return await GetUserProfile(uid);
        }

        public async Task<GetAccountInfoResponse> GetUserInfoByToken(string regToken)
        {
            return await GetUserProfile(regToken: regToken);
        }

        public async Task<BaseGigyaResponse> SetAccountInfo(string did, GigyaUserProfile profile = null,
            object data = null)
        {
            var serializationSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true
            };

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("apiKey", _configuration.ApiKey),
                new KeyValuePair<string, string>("secret", _configuration.SecretKey),
                new KeyValuePair<string, string>("UID", did)
            };

            if (profile != null)
            {
                var serializedProfile = JsonSerializer.Serialize(profile, serializationSettings);
                parameters.Add(new KeyValuePair<string, string>("profile", serializedProfile));
            }

            if (data != null)
            {
                var serializedData = JsonSerializer.Serialize(data, serializationSettings);
                parameters.Add(new KeyValuePair<string, string>("data", serializedData));
            }

            var setAccountDataMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.setAccountInfo"),
                new FormUrlEncodedContent(parameters));

            var setAccountResponse = await JsonSerializer.DeserializeAsync<BaseGigyaResponse>(
                await setAccountDataMessage.Content
                    .ReadAsStreamAsync());

            return setAccountResponse;
        }

        public async Task<LoginResponse> NotifyLogin(string did, string targetEnvironment = null)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("apiKey", _configuration.ApiKey),
                new KeyValuePair<string, string>("secret", _configuration.SecretKey),
                new KeyValuePair<string, string>("siteUID", did)
            };
            
            if(targetEnvironment !=null)
                parameters.Add(new KeyValuePair<string, string>("targetEnv", targetEnvironment));
            
            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.notifyLogin"),
                new FormUrlEncodedContent(parameters));

            return await JsonSerializer.DeserializeAsync<LoginResponse>(
                await responseMessage.Content.ReadAsStreamAsync());
        }

        public async Task<JsonWebKey> GetPublicKey()
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("apiKey", _configuration.ApiKey)
            };
            
            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.getJWTPublicKey"),
                new FormUrlEncodedContent(parameters));
            return new JsonWebKey(await responseMessage.Content.ReadAsStringAsync());
        }

        private async Task<GetAccountInfoResponse> GetUserProfile(string uid = null, string regToken = null)
        {
            var data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("apiKey", _configuration.ApiKey),
                new KeyValuePair<string, string>("secret", _configuration.SecretKey)
            };

            data.Add(!string.IsNullOrEmpty(uid)
                ? new KeyValuePair<string, string>("UID", uid)
                : new KeyValuePair<string, string>("regToken", regToken));

            var getAccountMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.getAccountInfo"),
                new FormUrlEncodedContent(data));

            var content =
                await JsonSerializer.DeserializeAsync<GetAccountInfoResponse>(await getAccountMessage.Content
                    .ReadAsStreamAsync());

            return content;
        }
    }
}