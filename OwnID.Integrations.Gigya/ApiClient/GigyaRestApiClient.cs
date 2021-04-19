using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OwnID.Extensibility.Extensions;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Logs;
using OwnID.Integrations.Gigya.Configuration;
using OwnID.Integrations.Gigya.Contracts;
using OwnID.Integrations.Gigya.Contracts.Accounts;
using OwnID.Integrations.Gigya.Contracts.Jwt;
using OwnID.Integrations.Gigya.Contracts.Login;

namespace OwnID.Integrations.Gigya.ApiClient
{
    public class GigyaRestApiClient<TProfile> where TProfile : class, IGigyaUserProfile
    {
        private readonly IGigyaConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GigyaRestApiClient<TProfile>> _logger;
        private readonly bool _logResponse;
        private readonly ConcurrentDictionary<GigyaFields, string> _profileFieldsCache = new();


        public GigyaRestApiClient(IGigyaConfiguration configuration, IHttpClientFactory httpClientFactory,
            ILogger<GigyaRestApiClient<TProfile>> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _logResponse = _logger.IsEnabled(LogLevel.Debug);
        }

        public async Task<GetAccountInfoResponse<TProfile>> GetUserInfoByUid(string uid)
        {
            var parameters = ParametersFactory.CreateAuthParameters(_configuration);

            if (!string.IsNullOrEmpty(uid))
                parameters.AddParameter("UID", uid);

            var getAccountMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.getAccountInfo"),
                new FormUrlEncodedContent(parameters));

            return await ParseAsync<GetAccountInfoResponse<TProfile>>(getAccountMessage.Content,
                () => $"GetUserInfoByUid('{uid}')");
            ;
        }

        public async Task<BaseGigyaResponse> SetAccountInfoAsync<T>(string did, T profile = null,
            AccountData data = null) where T : class, IGigyaUserProfile
        {
            var parameters = ParametersFactory.CreateAuthParameters(_configuration)
                .AddParameter("UID", did);

            if (profile != null)
                parameters.AddParameter("profile", profile);

            if (data != null)
            {
                foreach (var connection in data.OwnId.Connections.Where(connection =>
                    string.IsNullOrEmpty(connection.Hash)))
                    connection.Hash = connection.PublicKey.ToSha256();

                parameters.AddParameter("data", data);
            }

            var setAccountDataMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.setAccountInfo"),
                new FormUrlEncodedContent(parameters));

            return await ParseAsync<BaseGigyaResponse>(setAccountDataMessage.Content, () =>
            {
                var profileStr = profile != null ? OwnIdSerializer.Serialize(profile) : string.Empty;
                var dataStr = data != null ? OwnIdSerializer.Serialize(data) : string.Empty;
                return $"SetAccountInfoAsync('{did}', '{profileStr}', '{dataStr}')";
            });
        }

        public async Task<LoginResponse> NotifyLogin(string did, string targetEnvironment = null)
        {
            var parameters = ParametersFactory.CreateAuthParameters(_configuration)
                .AddParameter("siteUID", did)
                .AddParameter("skipValidation", bool.TrueString);

            if (!string.IsNullOrEmpty(targetEnvironment))
                parameters.AddParameter("targetEnv", targetEnvironment);

            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.notifyLogin"),
                new FormUrlEncodedContent(parameters));

            return await ParseAsync<LoginResponse>(responseMessage.Content,
                () => $"NotifyLogin('{did}', '{targetEnvironment}')");
        }

        public async Task<JsonWebKey> GetPublicKey()
        {
            var parameters = ParametersFactory.CreateApiKeyParameter(_configuration);

            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.getJWTPublicKey"),
                new FormUrlEncodedContent(parameters));
            var result = await responseMessage.Content.ReadAsStringAsync();
            _logger.Log(LogLevel.Debug, () => $"GetPublicKey() -> {result}");
            return new JsonWebKey(result);
        }

        public async Task<GetJwtResponse> GetJwt(string did)
        {
            var parameters = ParametersFactory.CreateAuthParameters(_configuration)
                .AddParameter("targetUID", did);

            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.getJWT"),
                new FormUrlEncodedContent(parameters));
            return await ParseAsync<GetJwtResponse>(responseMessage.Content, () => $"GetJwt('${did}')");
        }

        /// <summary>
        ///     Reset account password with resetToken generated by Gigya
        /// </summary>
        /// <param name="resetToken">reset token</param>
        /// <param name="newPassword">new password to set</param>
        /// <returns>
        ///     A task that represents the asynchronous reset password operation.
        ///     The task result contains the <see cref="ResetPasswordResponse" />
        /// </returns>
        /// <remarks>
        ///     Documentation: https://developers.gigya.com/display/GD/accounts.resetPassword+REST
        /// </remarks>
        public async Task<ResetPasswordResponse> ResetPasswordAsync(string resetToken, string newPassword)
        {
            var parameters = ParametersFactory.CreateAuthParameters(_configuration)
                .AddParameter("newPassword", newPassword)
                .AddParameter("passwordResetToken", resetToken);

            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.resetPassword"),
                new FormUrlEncodedContent(parameters));

            return await ParseAsync<ResetPasswordResponse>(responseMessage.Content,
                () => $"ResetPasswordAsync('{resetToken}', '{newPassword}')");
        }

        public async Task<BaseGigyaResponse> DeleteAccountAsync(string did)
        {
            var parameters = ParametersFactory.CreateAuthParameters(_configuration)
                .AddParameter("UID", did);

            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.deleteAccount"),
                new FormUrlEncodedContent(parameters));

            return await ParseAsync<BaseGigyaResponse>(responseMessage.Content, () => $"DeleteAccountAsync('{did}')");
        }

        public async Task<UidContainer> SearchByPublicKey(string publicKey,
            GigyaFields fields = GigyaFields.UID | GigyaFields.Connections)
        {
            var objectsToGet = fields | GigyaFields.ConnectionPublicKeys;
            var result =
                await SearchAsync<UidContainer>(GigyaFields.ConnectionPublicKeysHash, publicKey.ToSha256(),
                    objectsToGet);

            if (!result.IsSuccess
                || (result.First.Data?.OwnId?.Connections?.All(x => x.PublicKey != publicKey) ?? true))
                return null;

            return result.First;
        }

        public async Task<SearchGigyaResponse<TResult>> SearchAsync<TResult>(GigyaFields searchKey, string searchValue,
            GigyaFields resultSet = GigyaFields.UID | GigyaFields.Connections) where TResult : class
        {
            var query =
                $"SELECT {GetGigyaProfileFields(resultSet)} FROM accounts WHERE {GetGigyaProfileFields(searchKey)} = \"{searchValue}\" LIMIT 1";
            var parameters = ParametersFactory.CreateAuthParameters(_configuration).AddParameter("query", query);
            var responseMessage = await _httpClient.PostAsync(
                new Uri($"https://accounts.{_configuration.DataCenter}/accounts.search"),
                new FormUrlEncodedContent(parameters));
            return await ParseAsync<SearchGigyaResponse<TResult>>(responseMessage.Content,
                () => $"SearchAsync with query '{query}'");
        }

        private string GetGigyaProfileFields(GigyaFields fields)
        {
            if (_profileFieldsCache.TryGetValue(fields, out var result))
                return result;

            var resultFields = new List<string>();

            var enumType = typeof(GigyaFields);
            foreach (var value in Enum.GetValues<GigyaFields>())
            {
                if (!fields.HasFlag(value))
                    continue;

                var memberInfos = enumType.GetMember(value.ToString());

                var enumValueMemberInfo = memberInfos.First(m => m.DeclaringType == enumType);
                var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(GigyaFieldsAttribute), false);

                if (valueAttributes.Length == 0)
                    throw new Exception($"`{nameof(GigyaFields)}.{value}` doesn't has `GigyaFields` attribute");

                var queriedFields = ((GigyaFieldsAttribute) valueAttributes[0]).Fields.Split(",");

                resultFields.AddRange(queriedFields);
            }

            result = string.Join(", ", resultFields
                .Select(x => x.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToArray());

            _profileFieldsCache.TryAdd(fields, result);

            return result;
        }

        private async Task<T> ParseAsync<T>(HttpContent content, Func<string> prefixGen = null)
        {
            if (!_logResponse)
                return await OwnIdSerializer.DeserializeAsync<T>(await content.ReadAsStreamAsync());

            var json = await content.ReadAsStringAsync();
            var prefix = string.Empty;

            if (prefixGen != null)
                prefix = prefixGen();

            _logger.Log(LogLevel.Debug, $"Call {prefix} -> Response: {json}");
            return OwnIdSerializer.Deserialize<T>(json);
        }
    }
}