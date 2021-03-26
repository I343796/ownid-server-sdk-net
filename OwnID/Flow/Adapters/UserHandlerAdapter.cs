using System.Text.Json;
using System.Threading.Tasks;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Fido2;
using OwnID.Extensibility.Flow.Contracts.Internal;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Services;

namespace OwnID.Flow.Adapters
{
    public class UserHandlerAdapter<TProfile> : IUserHandlerAdapter where TProfile : class
    {
        private readonly IUserHandler<TProfile> _adaptee;
        private readonly JsonSerializerOptions _serializerOptions;

        public UserHandlerAdapter(IUserHandler<TProfile> adaptee)
        {
            _adaptee = adaptee;
            _serializerOptions = OwnIdSerializer.GetDefaultProperties();
            _serializerOptions.PropertyNamingPolicy = null;
        }

        public IFormContext CreateUserDefinedContext(UserProfileData profileData,
            ILocalizationService localizationService)
        {
            return new UserProfileFormContext<TProfile>(profileData.DID, profileData.PublicKey,
                OwnIdSerializer.Deserialize<TProfile>(profileData.Profile?.GetRawText(), _serializerOptions),
                localizationService);
        }

        public async Task CreateProfileAsync(IFormContext context, string recoveryToken = null,
            string recoveryData = null)
        {
            await _adaptee.CreateProfileAsync(context as UserProfileFormContext<TProfile>, recoveryToken, recoveryData);
        }

        public async Task UpdateProfileAsync(IFormContext context)
        {
            await _adaptee.UpdateProfileAsync(context as UserProfileFormContext<TProfile>);
        }

        public async Task<IdentitiesCheckResult> CheckUserIdentitiesAsync(string did, string publicKey)
        {
            return await _adaptee.CheckUserIdentitiesAsync(did, publicKey);
        }

        public Task<bool> IsUserExistsAsync(string publicKey)
        {
            return _adaptee.IsUserExists(publicKey);
        }

        public Task<bool> IsFido2UserExistsAsync(string fido2CredentialId)
        {
            return _adaptee.IsFido2UserExists(fido2CredentialId);
        }

        public Task<AuthResult<object>> RegisterPartialAsync(string did, OwnIdConnection ownIdConnection)
        {
            return _adaptee.RegisterPartialAsync(did, ownIdConnection);
        }

        public async Task<AuthResult<object>> OnSuccessLoginAsync(string did, string publicKey)
        {
            return await _adaptee.OnSuccessLoginAsync(did, publicKey);
        }

        public async Task<AuthResult<object>> OnSuccessLoginByPublicKeyAsync(string publicKey)
        {
            return await _adaptee.OnSuccessLoginByPublicKeyAsync(publicKey);
        }

        public Task<AuthResult<object>> OnSuccessLoginByFido2Async(string fido2CredentialId, uint fido2SignCounter)
        {
            return _adaptee.OnSuccessLoginByFido2Async(fido2CredentialId, fido2SignCounter);
        }

        public Task<Fido2Info> FindFido2InfoAsync(string fido2CredentialId)
        {
            return _adaptee.FindFido2Info(fido2CredentialId);
        }

        public async Task<ConnectionRecoveryResult<object>> GetConnectionRecoveryDataAsync(string recoveryToken,
            bool includingProfile = false)
        {
            var result = await _adaptee.GetConnectionRecoveryDataAsync(recoveryToken, includingProfile);

            if (result == null)
                return null;

            return new ConnectionRecoveryResult<object>
            {
                PublicKey = result.PublicKey,
                RecoveryData = result.RecoveryData,
                DID = result.DID,
                UserProfile = result.UserProfile
            };
        }

        public Task<string> GetUserIdByEmail(string email)
        {
            return _adaptee.GetUserIdByEmail(email);
        }

        public Task<UserSettings> GetUserSettingsAsync(string publicKey)
        {
            return _adaptee.GetUserSettingsAsync(publicKey);
        }

        public Task<string> GetUserNameAsync(string did)
        {
            return _adaptee.GetUserNameAsync(did);
        }

        public Task UpgradeConnectionAsync(string publicKey, OwnIdConnection newConnection)
        {
            return _adaptee.UpgradeConnectionAsync(publicKey, newConnection);
        }
    }
}