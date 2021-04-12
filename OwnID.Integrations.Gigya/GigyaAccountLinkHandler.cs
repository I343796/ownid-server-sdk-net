using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Flow.Contracts.Link;
using OwnID.Extensibility.Json;
using OwnID.Integrations.Gigya.ApiClient;
using OwnID.Integrations.Gigya.Configuration;
using OwnID.Integrations.Gigya.Contracts;
using OwnID.Integrations.Gigya.Contracts.Accounts;

namespace OwnID.Integrations.Gigya
{
    public class GigyaAccountLinkHandler<TProfile> : IAccountLinkHandler
        where TProfile : class, IGigyaUserProfile
    {
        private const string ApiKeyPayloadKey = "apiKey";
        private readonly IGigyaConfiguration _configuration;
        private readonly IOwnIdCoreConfiguration _ownIdConfiguration;
        private readonly GigyaRestApiClient<TProfile> _restApiClient;

        public GigyaAccountLinkHandler(GigyaRestApiClient<TProfile> restApiClient, IGigyaConfiguration configuration,
            IOwnIdCoreConfiguration ownIdConfiguration)
        {
            _restApiClient = restApiClient;
            _configuration = configuration;
            _ownIdConfiguration = ownIdConfiguration;
        }

        public async Task<LinkState> GetCurrentUserLinkStateAsync(string payload)
        {
            var jwt = OwnIdSerializer.Deserialize<JwtContainer>(payload)?.Jwt;

            if (string.IsNullOrEmpty(jwt))
                throw new Exception("No JWT was found in HttpRequest");

            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(jwt)) throw new Exception("Invalid jwt");

            var rsaSecurityKey = await _restApiClient.GetPublicKey();

            try
            {
                tokenHandler.ValidateToken(jwt, new TokenValidationParameters
                {
                    IssuerSigningKey = rsaSecurityKey,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateIssuer = false
                    // TODO: add issuer to token for validation
                }, out _);
            }
            catch (SecurityTokenValidationException ex)
            {
                throw new Exception($"Token failed validation: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                throw new Exception($"Token was invalid: {ex.Message}");
            }

            var token = tokenHandler.ReadJwtToken(jwt);

            if (!token.Payload.ContainsKey(ApiKeyPayloadKey) ||
                (string) token.Payload[ApiKeyPayloadKey] != _configuration.ApiKey)
                throw new Exception("Jwt was created for different apiKey");

            var did = token.Subject;

            var accountInfo = await _restApiClient.SearchAsync<AccountInfoResponse<TProfile>>(GigyaFields.UID, did);

            if (!accountInfo.IsSuccess)
            {
                if (accountInfo.ErrorCode != 0)
                    throw new Exception(accountInfo.GetFailureMessage());
                
                throw new Exception($"Can't find user with did = {did}");
            }

            return new LinkState(did, (uint) (accountInfo.First.Data?.OwnId?.Connections?.Count ?? 0));
        }

        public async Task OnLinkAsync(string did, OwnIdConnection connection)
        {
            var accountInfo = await _restApiClient.SearchAsync<UidContainer>(GigyaFields.UID, did);

            if (!accountInfo.IsSuccess)
            {
                if (accountInfo.ErrorCode != 0)
                    throw new Exception(accountInfo.GetFailureMessage());
                
                throw new Exception($"Can't find user with did = {did}");
            }

            accountInfo.First.Data ??= new AccountData();

            if (accountInfo.First.Data.OwnId.Connections.Count >= _ownIdConfiguration.MaximumNumberOfConnectedDevices)
                throw new Exception(
                    $"Gigya.OnLink error -> maximum number ({_ownIdConfiguration.MaximumNumberOfConnectedDevices}) of linked devices reached");

            // add new public key to connection 
            accountInfo.First.Data.OwnId.Connections.Add(new GigyaOwnIdConnection(connection));

            var setAccountMessage =
                await _restApiClient.SetAccountInfoAsync<TProfile>(did, data: accountInfo.First.Data);

            if (setAccountMessage.ErrorCode != 0)
                throw new Exception(
                    $"Gigya.setAccountInfo error -> {setAccountMessage.GetFailureMessage()}");
        }
    }
}