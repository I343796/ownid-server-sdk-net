using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using OwnIdSdk.NetCore3.Configuration;
using OwnIdSdk.NetCore3.Contracts.Jwt;
using OwnIdSdk.NetCore3.Cryptography;
using OwnIdSdk.NetCore3.Extensions;
using OwnIdSdk.NetCore3.Flow;
using OwnIdSdk.NetCore3.Store;

namespace OwnIdSdk.NetCore3
{
    /// <summary>
    ///     Base OwnId logic provider
    /// </summary>
    public class OwnIdProvider
    {
        private readonly ICacheStore _cacheStore;
        private readonly ILocalizationService _localizationService;
        private readonly IOwnIdCoreConfiguration _ownIdCoreConfiguration;

        /// <summary>
        /// </summary>
        /// <param name="ownIdCoreConfiguration">Core configuration to be used</param>
        /// <param name="cacheStore"><see cref="CacheItem" /> store</param>
        /// <param name="localizationService">Optional(only if localization is needed). Localization service</param>
        public OwnIdProvider([NotNull] IOwnIdCoreConfiguration ownIdCoreConfiguration, [NotNull] ICacheStore cacheStore,
            ILocalizationService localizationService)
        {
            _cacheStore = cacheStore;
            _localizationService = localizationService;
            _ownIdCoreConfiguration = ownIdCoreConfiguration;
        }

        /// <summary>
        ///     Generates new unique identifier
        /// </summary>
        public string GenerateContext()
        {
            return Guid.NewGuid().ToShortString();
        }

        /// <summary>
        ///     Generates nonce
        /// </summary>
        public string GenerateNonce()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        ///     Generates JWT with configuration and user profile for account linking process
        /// </summary>
        /// <param name="context">Challenge Unique identifier</param>
        /// <param name="nextStep">Next flow step description</param>
        /// <param name="did">User unique identity</param>
        /// <param name="profile">User profile</param>
        /// <param name="locale">Optional. Content locale</param>
        /// <param name="includeRequester">Optional. Default = false. True if should add requester info to jwt</param>
        /// <returns>Base64 encoded string that contains JWT</returns>
        public string GenerateProfileWithConfigDataJwt(string context, Step nextStep, string did,
            object profile,
            string locale = null, bool includeRequester = false)
        {
            var data = GetFieldsConfigDictionary(did).Union(GetProfileDataDictionary(profile))
                .ToDictionary(x => x.Key, x => x.Value);

            if (includeRequester)
            {
                var (key, value) = GetRequester();
                data[key] = value;
            }

            var fields = GetBaseFlowFieldsDictionary(context, nextStep, data,
                locale);

            return GenerateDataJwt(fields);
        }


        /// <summary>
        ///     Creates JWT challenge with requested information by OwnId app
        /// </summary>
        /// <param name="context">Challenge Unique identifier</param>
        /// <param name="nextStep">Next flow step description</param>
        /// <param name="locale">Optional. Content locale</param>
        /// <param name="includeRequester">Optional. Default = false. True if should add requester info to jwt</param>
        /// <returns>Base64 encoded string that contains JWT with hash</returns>
        public string GenerateProfileConfigJwt(string context, Step nextStep, string locale = null,
            bool includeRequester = false)
        {
            var data = GetFieldsConfigDictionary(GenerateUserDid());

            if (includeRequester)
            {
                var (key, value) = GetRequester();
                data[key] = value;
            }

            var fields = GetBaseFlowFieldsDictionary(context, nextStep, data,
                locale);

            return GenerateDataJwt(fields);
        }

        /// <summary>
        ///     Generates JWT for pin step
        /// </summary>
        /// <param name="context">Challenge Unique identifier</param>
        /// <param name="nextStep">Next flow step description</param>
        /// <param name="pin">PIN code generated by <see cref="SetSecurityCode" /></param>
        /// <param name="locale">Optional. Content locale</param>
        /// <returns></returns>
        public string GeneratePinStepJwt(string context, Step nextStep, string pin,
            string locale = null)
        {
            var requester = GetRequester();

            var data = new Dictionary<string, object>
            {
                {"pin", pin},
                {requester.Key, requester.Value}
            };

            var fields = GetBaseFlowFieldsDictionary(context, nextStep, data, locale);
            return GenerateDataJwt(fields);
        }

        public string GenerateFinalStepJwt(string context, Step nextStep, string locale = null)
        {
            var fields = GetBaseFlowFieldsDictionary(context, nextStep, null, locale);
            return GenerateDataJwt(fields);
        }

        public string GeneratePartialDidStep(string context, Step nextStep, string locale = null)
        {
            var (didKey, didValue) = GetDid(GenerateUserDid());
            var (reqKey, reqValue) = GetRequester();
            var fields = GetBaseFlowFieldsDictionary(context, nextStep,
                new Dictionary<string, object>
                    {{didKey, didValue}, {reqKey, reqValue}, {"requestedFields", new object[0]}}, locale);

            return GenerateDataJwt(fields);
        }

        /// <summary>
        ///     Decodes provided by OwnId application JWT with data
        /// </summary>
        /// <param name="jwt">Base64 JWT string </param>
        /// <typeparam name="TData">Data type used as model for deserialization</typeparam>
        /// <returns>Context(challenge unique identifier) with <see cref="UserProfileData" /></returns>
        /// <exception cref="Exception">If something went wrong during token validation</exception>
        public (string, TData) GetDataFromJwt<TData>(string jwt) where TData : ISignedData
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(jwt)) throw new Exception("invalid jwt");

            var token = tokenHandler.ReadJwtToken(jwt);
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            var data = JsonSerializer.Deserialize<TData>(token.Payload["data"].ToString(), serializerOptions);
            // TODO: add type of challenge
            using var sr = new StringReader(data.PublicKey);
            var rsaSecurityKey = new RsaSecurityKey(RsaHelper.LoadKeys(sr));

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

            return (token.Id, data);
        }

        /// <summary>
        ///     Creates auth flow session item and saves it by <paramref name="context" /> into <see cref="ICacheStore" />
        /// </summary>
        /// <param name="context">Challenge unique identifier</param>
        /// <param name="nonce">Nonce</param>
        /// <param name="challengeType">Requested challenge type</param>
        /// <param name="flowType">Flow type for OwnID process</param>
        /// <param name="did">User unique identity, should be null for register or login</param>
        /// <param name="payload">payload</param>
        public async Task CreateAuthFlowSessionItemAsync(string context, string nonce, ChallengeType challengeType,
            FlowType flowType,
            string did = null, string payload = null)
        {
            await _cacheStore.SetAsync(context, new CacheItem
            {
                ChallengeType = challengeType,
                Nonce = nonce,
                Context = context,
                DID = did,
                Payload = payload,
                FlowType = flowType
            }
            ,TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.CacheExpirationTimeout));
        }

        /// <summary>
        ///     Sets Web App request/response token to check with the next request
        /// </summary>
        /// <param name="context">Challenge unique identifier</param>
        /// <param name="requestToken">Web App request token</param>
        /// <param name="responseToken">Server-side response token</param>
        /// <exception cref="ArgumentException">
        ///     If no <see cref="CacheItem" /> was found with <paramref name="context" />
        /// </exception>
        public async Task SetSecurityTokensAsync(string context, string requestToken, string responseToken)
        {
            var cacheItem = await _cacheStore.GetAsync(context);

            if (cacheItem == null)
                throw new ArgumentException($"Can not find any item with context '{context}'");

            cacheItem.RequestToken = requestToken;
            cacheItem.ResponseToken = responseToken;

            // TODO: move somewhere and rework logic
            if (cacheItem.Status == CacheItemStatus.Initiated)
                cacheItem.Status = CacheItemStatus.Started;

            await _cacheStore.SetAsync(context, cacheItem, TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.CacheExpirationTimeout));
        }

        /// <summary>
        ///     Sets approval result
        /// </summary>
        /// <param name="context">Challenge Unique identifier</param>
        /// <param name="nonce">Nonce</param>
        /// <param name="isApproved">True if approved</param>
        /// <exception cref="ArgumentException">Cache item was not found</exception>
        /// <exception cref="ArgumentException">Cache item has incorrect status to set resolution</exception>
        public async Task SetApprovalResolutionAsync(string context, string nonce, bool isApproved)
        {
            var cacheItem = await _cacheStore.GetAsync(context);

            if (cacheItem == null || cacheItem.Context != context || cacheItem.Nonce != nonce)
                throw new ArgumentException($"Can not find any item with context '{context}'");

            if (cacheItem.Status != CacheItemStatus.WaitingForApproval)
                throw new ArgumentException($"Incorrect status={cacheItem.Status.ToString()} for approval '{context}'");

            cacheItem.Status = isApproved ? CacheItemStatus.Approved : CacheItemStatus.Declined;
            await _cacheStore.SetAsync(context, cacheItem, TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.CacheExpirationTimeout));
        }

        public async Task SetPublicKeyAsync(string context, string publicKey, ChallengeType? actualChallengeType = null)
        {
            var cacheItem = await _cacheStore.GetAsync(context);

            if (cacheItem == null || cacheItem.Context != context)
                throw new ArgumentException($"Can not find any item with context '{context}'");

            if (cacheItem.FlowType != FlowType.PartialAuthorize)
                throw new ArgumentException($"Can not set public key for FlowType != PartialAuthorize for context '{context}'");
            
            if (cacheItem.Status != CacheItemStatus.Started)
                throw new ArgumentException($"Incorrect status={cacheItem.Status.ToString()} for setting public key for context '{context}'");
            
            if(cacheItem.ChallengeType != ChallengeType.Register && cacheItem.ChallengeType != ChallengeType.Login)
                throw new ArgumentException($"Can not update actual challenge type from {cacheItem.ChallengeType.ToString()} for setting public key for context '{context}'");

            if (actualChallengeType.HasValue)
            {
                if (actualChallengeType != ChallengeType.Login && actualChallengeType != ChallengeType.Register)
                    throw new ArgumentException(
                        $"Wrong new actual challenge type  {cacheItem.ChallengeType.ToString()} for setting public key for context '{context}'");
                cacheItem.ChallengeType = actualChallengeType.Value;
            }
            
            cacheItem.PublicKey = publicKey;
            
            await _cacheStore.SetAsync(context, cacheItem,
                TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.CacheExpirationTimeout));
        }

        /// <summary>
        ///     Try to find auth flow session item by <paramref name="context" /> in <see cref="ICacheStore" /> mark it as finish
        /// </summary>
        /// <param name="context">Challenge unique identifier</param>
        /// <param name="did">User unique identifier</param>
        /// <exception cref="ArgumentException">
        ///     If no <see cref="CacheItem" /> was found with <paramref name="context" />
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     If you try to finish session with different user DID
        /// </exception>
        public async Task FinishAuthFlowSessionAsync(string context, string did)
        {
            var cacheItem = await _cacheStore.GetAsync(context);

            if (cacheItem == null)
                throw new ArgumentException($"Can not find any item with context '{context}'");

            if (cacheItem.ChallengeType == ChallengeType.Link && cacheItem.DID != did)
                throw new ArgumentException($"Wrong user for linking {did}");

            if (cacheItem.HasFinalState)
                throw new ArgumentException(
                    $"Cache item with context='{context}' has final status={cacheItem.Status.ToString()}");

            cacheItem.DID = did;
            cacheItem.Status = CacheItemStatus.Finished;
            await _cacheStore.SetAsync(context, cacheItem, TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.CacheExpirationTimeout));
        }

        /// <summary>
        ///     Tries to find <see cref="CacheItem" /> by <paramref name="nonce" /> and <paramref name="context" /> in
        ///     <see cref="ICacheStore" /> and remove item if find operation was successful
        /// </summary>
        /// <param name="context">Challenge unique identifier</param>
        /// <param name="nonce">Nonce</param>
        /// <returns>
        ///     <see cref="CacheItemStatus" /> and <c>did</c> if <see cref="CacheItem" /> was found, otherwise null
        /// </returns>
        public async Task<CacheItem> PopFinishedAuthFlowSessionAsync(
            string context,
            string nonce)
        {
            var cacheItem = await _cacheStore.GetAsync(context);

            if (cacheItem == null
                || cacheItem.Nonce != nonce
                || cacheItem.Status == CacheItemStatus.Finished && string.IsNullOrEmpty(cacheItem.DID)
            )
                return null;

            // If finished - clear cache
            if (cacheItem.Status == CacheItemStatus.Finished)
                await _cacheStore.RemoveAsync(context);
            
            return cacheItem.Clone() as CacheItem;
        }

        /// <summary>
        ///     Tries to find <see cref="CacheItem" /> by <paramref name="context" /> in <see cref="ICacheStore" />
        /// </summary>
        /// <param name="context">Challenge unique identifier</param>
        /// <returns><see cref="CacheItem" /> or null</returns>
        public async Task<CacheItem> GetCacheItemByContextAsync(string context)
        {
            return (await _cacheStore.GetAsync(context))?.Clone() as CacheItem;
        }

        /// <summary>
        ///     Verifies if provided <paramref name="context" /> is valid
        /// </summary>
        /// <param name="context">Challenge unique identifier</param>
        /// <returns>True if valid</returns>
        public bool IsContextFormatValid(string context)
        {
            return Regex.IsMatch(context, "^([a-zA-Z0-9_-]{22})$");
        }

        /// <summary>
        ///     Sets security code to cache item and changes status to <see cref="CacheItemStatus.WaitingForApproval" />
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> SetSecurityCode(string context)
        {
            var random = new Random();
            var pin = random.Next(0, 9999).ToString("D4");
            var cacheItem = await _cacheStore.GetAsync(context);

            if (cacheItem == null)
                throw new ArgumentException($"Can not find any item with context '{context}'");

            if (cacheItem.Status != CacheItemStatus.Initiated && cacheItem.Status != CacheItemStatus.Started)
                throw new ArgumentException(
                    $"Wrong status '{cacheItem.Status.ToString()}' for cache item with context '{context}' to set PIN");

            if (cacheItem.ConcurrentId != null)
                throw new ArgumentException($"Context '{context}' is already modified with PIN");

            cacheItem.ConcurrentId = Guid.NewGuid().ToString();
            cacheItem.SecurityCode = pin;
            cacheItem.Status = CacheItemStatus.WaitingForApproval;

            await _cacheStore.SetAsync(context, cacheItem, TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.CacheExpirationTimeout));

            return pin;
        }

        /// <summary>
        ///     Gets hash of Base64 encoded JWT string
        /// </summary>
        /// <param name="jwt">Base64 encoded JWT string</param>
        /// <returns>SHA1 Base64 encoded string</returns>
        public string GetJwtHash(string jwt)
        {
            using var sha1 = new SHA1Managed();

            var b64 = Encoding.UTF8.GetBytes(jwt);
            var hash = sha1.ComputeHash(b64);

            return Convert.ToBase64String(hash);
        }

        private string GenerateDataJwt(Dictionary<string, object> data)
        {
            var rsaSecurityKey = new RsaSecurityKey(_ownIdCoreConfiguration.JwtSignCredentials);
            var tokenHandler = new JwtSecurityTokenHandler();

            //TODO: should be received from the user's phone
            var issuedAt = DateTime.UtcNow.Add(TimeSpan.FromHours(-1));
            var notBefore = issuedAt;
            var expires = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(_ownIdCoreConfiguration.JwtExpirationTimeout));

            var payload = new JwtPayload(null, null, null, notBefore, expires, issuedAt);

            foreach (var (key, value) in data) payload.Add(key, value);

            var jwt = new JwtSecurityToken(
                new JwtHeader(new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256)), payload);

            return tokenHandler.WriteToken(jwt);
        }

        private Dictionary<string, object> GetProfileDataDictionary(object profile)
        {
            return new Dictionary<string, object> {{"profile", profile}};
        }

        private Dictionary<string, object> GetBaseFlowFieldsDictionary(string context, Step nextStep,
            object data = null, string locale = null)
        {
            var stepType = nextStep.Type.ToString();
            var actionType = nextStep.ActionType.ToString();
            var stepDict = new Dictionary<string, object>
            {
                {"type", stepType.First().ToString().ToLowerInvariant() + stepType.Substring(1)},
                {"actionType", actionType.First().ToString().ToLowerInvariant() + actionType.Substring(1)},
                {"challengeType", nextStep.ChallengeType.ToString().ToLowerInvariant()}
            };

            if (nextStep.Polling != null)
                stepDict.Add("polling", new
                {
                    url = nextStep.Polling.Url.ToString(),
                    method = nextStep.Polling.Method,
                    interval = nextStep.Polling.Interval
                });

            if (nextStep.Callback != null)
                stepDict.Add("callback", new
                {
                    url = nextStep.Callback.Url,
                    method = nextStep.Callback.Method
                });

            var fields = new Dictionary<string, object>
            {
                {"jti", context},
                {"locale", locale},
                {"nextStep", stepDict}
            };

            if (data != null)
                fields.Add("data", data);

            return fields;
        }

        private Dictionary<string, object> GetFieldsConfigDictionary(string did)
        {
            var dataFields = new Dictionary<string, object>
            {
                {
                    // TODO : PROFILE
                    "requestedFields", _ownIdCoreConfiguration.ProfileConfiguration.ProfileFieldMetadata.Select(x =>
                    {
                        var label = Localize(x.Label, true);

                        return new
                        {
                            type = x.Type,
                            key = x.Key,
                            label,
                            placeholder = Localize(x.Placeholder, true),
                            validators = x.Validators.Select(v => new
                            {
                                type = v.Type,
                                errorMessage = string.Format(v.NeedsInternalLocalization
                                    ? Localize(v.GetErrorMessageKey(), true)
                                    : v.GetErrorMessageKey(), label)
                            })
                        };
                    })
                }
            };

            var (didKey, didValue) = GetDid(did);
            dataFields.Add(didKey, didValue);

            return dataFields;
        }

        private KeyValuePair<string, string> GetDid(string did)
        {
            return new KeyValuePair<string, string>("did", did);
        }

        private KeyValuePair<string, object> GetRequester()
        {
            return new KeyValuePair<string, object>("requester", new
            {
                did = _ownIdCoreConfiguration.DID,
                pubKey = RsaHelper.ExportPublicKeyToPkcsFormattedString(_ownIdCoreConfiguration
                    .JwtSignCredentials),
                name = Localize(_ownIdCoreConfiguration.Name),
                icon = _ownIdCoreConfiguration.Icon,
                description = Localize(_ownIdCoreConfiguration.Description)
            });
        }

        private string GenerateUserDid()
        {
            return Guid.NewGuid().ToString();
        }

        private string Localize(string key, bool defaultAsAlternative = false)
        {
            return _localizationService?.GetLocalizedString(key) ?? key;
        }
    }
}