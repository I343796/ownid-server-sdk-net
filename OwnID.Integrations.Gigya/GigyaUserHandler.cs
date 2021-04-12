using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Fido2;
using OwnID.Extensibility.Flow.Contracts.Internal;
using OwnID.Integrations.Gigya.ApiClient;
using OwnID.Integrations.Gigya.Configuration;
using OwnID.Integrations.Gigya.Contracts;
using OwnID.Integrations.Gigya.Contracts.Accounts;

namespace OwnID.Integrations.Gigya
{
    public class GigyaUserHandler<TProfile> : IUserHandler<TProfile> where TProfile : class, IGigyaUserProfile
    {
        private readonly IGigyaConfiguration _configuration;
        private readonly ILogger<GigyaUserHandler<TProfile>> _logger;
        private readonly GigyaRestApiClient<TProfile> _restApiClient;

        public GigyaUserHandler(GigyaRestApiClient<TProfile> restApiClient, IGigyaConfiguration configuration,
            ILogger<GigyaUserHandler<TProfile>> logger)
        {
            _restApiClient = restApiClient;
            _configuration = configuration;
            _logger = logger;
        }

        public Task<AuthResult<object>> RegisterPartialAsync(string did, OwnIdConnection ownIdConnection)
        {
            return Task.FromException<AuthResult<object>>(new NotSupportedException());
        }

        public async Task<AuthResult<object>> OnSuccessLoginAsync(string did, string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                return await OnSuccessLoginInternalAsync(did);

            return await OnSuccessLoginByPublicKeyAsync(publicKey);
        }

        public async Task<AuthResult<object>> OnSuccessLoginByPublicKeyAsync(string publicKey)
        {
            var user = await _restApiClient.SearchByPublicKey(publicKey, GigyaFields.UID);

            if (string.IsNullOrEmpty(user?.UID))
                return new AuthResult<object>("Can not find user in Gigya search result with provided public key");

            return await OnSuccessLoginInternalAsync(user.UID);
        }

        public async Task<AuthResult<object>> OnSuccessLoginByFido2Async(string fido2CredentialId,
            uint fido2SignCounter)
        {
            var searchResult = await _restApiClient.SearchAsync<UidContainer>(GigyaFields.ConnectionFido2CredentialId,
                fido2CredentialId);
            
            if (!searchResult.IsSuccess)
            {
                if (searchResult.ErrorCode != 0)
                    throw new Exception(searchResult.GetFailureMessage());
                
                return new AuthResult<object>("Can not find user in Gigya with provided fido2 user id");
            }

            var connectionToUpdate = searchResult.First.Data.OwnId.Connections.First(x => x.Fido2CredentialId == fido2CredentialId);

            // Update signature counter
            connectionToUpdate.Fido2SignatureCounter = fido2SignCounter.ToString();

            var setAccountResponse = await _restApiClient.SetAccountInfoAsync(searchResult.First.UID, (TProfile) null, searchResult.First.Data);
            if (setAccountResponse.ErrorCode > 0)
                throw new Exception(
                    $"did: {searchResult.First.UID} " +
                    $"Gigya.setAccountInfo for EXISTING user error -> {setAccountResponse.GetFailureMessage()}");

            return await OnSuccessLoginInternalAsync(searchResult.First.UID);
        }

        public async Task UpgradeConnectionAsync(string publicKey, OwnIdConnection newConnection)
        {
            var user = await _restApiClient.SearchByPublicKey(publicKey);
            if (user == null)
                return;

            // Remove old connection
            var connectionToRemove = user.Data.OwnId.Connections.First(x => x.PublicKey == publicKey);
            user.Data.OwnId.Connections.Remove(connectionToRemove);

            // Add new one
            user.Data.OwnId.Connections.Add(new GigyaOwnIdConnection(newConnection));

            var setAccountResponse = await _restApiClient.SetAccountInfoAsync(user.UID, (TProfile) null, user.Data);
            if (setAccountResponse.ErrorCode > 0)
                throw new Exception(
                    $"did: {user.UID} " +
                    $"Gigya.setAccountInfo for EXISTING user error -> {setAccountResponse.GetFailureMessage()}");
        }

        public async Task<string> GetUserNameAsync(string did)
        {
            var getAccountMessage =
                await _restApiClient.SearchAsync<AccountInfoResponse<TProfile>>(GigyaFields.UID, did,
                    GigyaFields.Profile);

            if (getAccountMessage.IsSuccess) 
                return getAccountMessage.First.Profile?.FirstName;
            
            if (getAccountMessage.ErrorCode != 0)
                throw new Exception(getAccountMessage.GetFailureMessage());
                
            throw new Exception($"Can't find user with did = {did}");
        }

        public async Task<IdentitiesCheckResult> CheckUserIdentitiesAsync(string did, string publicKey)
        {
            var getAccountMessage = await _restApiClient.GetUserInfoByUid(did);

            if (getAccountMessage.ErrorCode == 403005)
                return IdentitiesCheckResult.UserNotFound;

            if (getAccountMessage.ErrorCode != 0)
                throw new Exception(
                    $"Gigya.notifyLogin error -> {getAccountMessage.GetFailureMessage()}");

            if (getAccountMessage.Data == null || !getAccountMessage.Data.OwnId.Connections.Any())
            {
                _logger.LogError($"Found gigya user uid={did} without pubKey");
                return IdentitiesCheckResult.WrongPublicKey;
            }

            if (getAccountMessage.Data.OwnId.Connections.All(x => x.PublicKey != publicKey))
            {
                _logger.LogError("Public key doesn't match any of gigya user keys");
                return IdentitiesCheckResult.WrongPublicKey;
            }

            return IdentitiesCheckResult.UserExists;
        }

        public async Task<bool> IsUserExists(string publicKey)
        {
            var user = await _restApiClient.SearchByPublicKey(publicKey, GigyaFields.UID);
            return !string.IsNullOrWhiteSpace(user?.UID);
        }

        public async Task<bool> IsFido2UserExists(string fido2CredentialId)
        {
            if (string.IsNullOrWhiteSpace(fido2CredentialId))
                return false;

            var searchResult = await _restApiClient.SearchAsync<AccountInfoResponse<TProfile>>(
                GigyaFields.ConnectionFido2CredentialId, fido2CredentialId);
            return searchResult.IsSuccess;
        }

        public async Task<Fido2Info> FindFido2Info(string fido2CredentialId)
        {
            var searchResult = await _restApiClient.SearchAsync<AccountInfoResponse<TProfile>>(
                GigyaFields.ConnectionFido2CredentialId, fido2CredentialId);

            if (!searchResult.IsSuccess)
                return null;

            var connection =
                searchResult.First.Data.OwnId.Connections.FirstOrDefault(c => c.Fido2CredentialId == fido2CredentialId);

            if (string.IsNullOrEmpty(connection?.Fido2SignatureCounter)
                || !uint.TryParse(connection.Fido2SignatureCounter, out var signatureCounter))
                throw new Exception(
                    $"connection with fido2 credential id '{fido2CredentialId}' doesn't have signature count");

            return new Fido2Info
            {
                UserId = searchResult.First.DID,
                PublicKey = connection.PublicKey,
                SignatureCounter = signatureCounter,
                CredentialId = connection.Fido2CredentialId
            };
        }

        public async Task<ConnectionRecoveryResult<TProfile>> GetConnectionRecoveryDataAsync(string recoveryToken,
            bool includingProfile = false)
        {
            var resultSet = GigyaFields.UID | GigyaFields.Connections;

            if (includingProfile)
                resultSet |= GigyaFields.Profile;

            var result =
                await _restApiClient.SearchAsync<AccountInfoResponse<TProfile>>(GigyaFields.ConnectionRecoveryId,
                    recoveryToken, resultSet);

            if (!result.IsSuccess)
            {
                if (result.ErrorCode != 0)
                    _logger.LogError(result.GetFailureMessage());
                else
                    _logger.LogError($"Can not find connection recovery token in Gigya {recoveryToken}");

                return null;
            }

            var connection = result.First.Data.OwnId.Connections.First(x => x.RecoveryToken == recoveryToken);

            return new ConnectionRecoveryResult<TProfile>
            {
                PublicKey = connection.PublicKey,
                DID = result.First.DID,
                RecoveryData = connection.RecoveryData,
                UserProfile = includingProfile ? result.First.Profile : null
            };
        }

        public async Task<string> GetUserIdByEmail(string email)
        {
            var result =
                await _restApiClient.SearchAsync<UidContainer>(GigyaFields.ProfileEmail, email, GigyaFields.UID);
            return result.First?.UID;
        }

        public async Task<UserSettings> GetUserSettingsAsync(string publicKey)
        {
            var user = await _restApiClient.SearchByPublicKey(publicKey, GigyaFields.Settings);
            return user?.Data?.OwnId?.UserSettings;
        }

        public async Task CreateProfileAsync(IUserProfileFormContext<TProfile> context, string recoveryToken = null,
            string recoveryData = null)
        {
            var loginResponse = await _restApiClient.NotifyLogin(context.DID);

            if (loginResponse.ErrorCode != 0)
                throw new Exception(
                    $"Gigya.notifyLogin error -> {loginResponse.GetFailureMessage()}");

            var connection = new GigyaOwnIdConnection
            {
                PublicKey = context.PublicKey,
                RecoveryToken = recoveryToken,
                RecoveryData = recoveryData
            };

            var setAccountMessage =
                await _restApiClient.SetAccountInfoAsync(context.DID, context.Profile, new AccountData(connection));

            if (setAccountMessage.ErrorCode > 0)
            {
                _logger.LogError($"did: {context.DID} " +
                                 $"Gigya.setAccountInfo (profile) for NEW user error -> {setAccountMessage.GetFailureMessage()}");

                var removeUserResult = await _restApiClient.DeleteAccountAsync(context.DID);

                if (removeUserResult.ErrorCode != 0)
                    throw new Exception(
                        $"Gigya.deleteAccount with uid={context.DID} error -> {removeUserResult.GetFailureMessage()}");

                SetErrorsToContext(context, setAccountMessage);
            }
        }

        public async Task UpdateProfileAsync(IUserProfileFormContext<TProfile> context)
        {
            var setAccountResponse = await _restApiClient.SetAccountInfoAsync(context.DID, context.Profile);

            if (setAccountResponse.ErrorCode > 0)
            {
                _logger.LogError(
                    $"did: {context.DID} " +
                    $"Gigya.setAccountInfo for EXISTING user error -> {setAccountResponse.GetFailureMessage()}");

                SetErrorsToContext(context, setAccountResponse);
            }
        }

        private async Task<AuthResult<object>> OnSuccessLoginInternalAsync(string did)
        {
            if (_configuration.LoginType == GigyaLoginType.Session)
            {
                var loginResponse = await _restApiClient.NotifyLogin(did, "browser");

                if (loginResponse.SessionInfo == null || loginResponse.ErrorCode != 0)
                    return new AuthResult<object>($"Gigya: {loginResponse.GetFailureMessage()}");

                return new AuthResult<object>(new
                {
                    sessionInfo = loginResponse.SessionInfo
                });
            }

            var jwtResponse = await _restApiClient.GetJwt(did);

            if (jwtResponse.IdToken == null || jwtResponse.ErrorCode != 0)
                return new AuthResult<object>($"Gigya: {jwtResponse.GetFailureMessage()}");

            return new AuthResult<object>(new
            {
                idToken = jwtResponse.IdToken
            });
        }

        private static void SetErrorsToContext(IUserProfileFormContext<TProfile> context, BaseGigyaResponse response)
        {
            if (response.ValidationErrors != null && response.ValidationErrors.Any())
                foreach (var validationError in response.ValidationErrors)
                    //
                    // TODO: find better way to map field name to profile property
                    //
                    switch (validationError.FieldName)
                    {
                        case "profile.email":
                            context.SetError(x => x.Email, validationError.Message);
                            break;
                        default:
                            context.SetGeneralError(validationError.Message);
                            break;
                    }
            else
                context.SetGeneralError(response.UserFriendlyFailureMessage);
        }
    }
}