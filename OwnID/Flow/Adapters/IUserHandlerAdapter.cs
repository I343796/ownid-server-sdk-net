using System.Threading.Tasks;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Fido2;
using OwnID.Extensibility.Flow.Contracts.Internal;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Services;

namespace OwnID.Flow.Adapters
{
    public interface IUserHandlerAdapter
    {
        IFormContext CreateUserDefinedContext(UserProfileData profileData, ILocalizationService localizationService);

        Task CreateProfileAsync(IFormContext context, string recoveryToken = null, string recoveryData = null);

        Task UpdateProfileAsync(IFormContext context);

        Task<IdentitiesCheckResult> CheckUserIdentitiesAsync(string did, string publicKey);

        Task<bool> IsUserExistsAsync(string publicKey);

        Task<bool> IsFido2UserExistsAsync(string fido2CredentialId);
        
        Task<AuthResult<object>> RegisterPartialAsync(string did, OwnIdConnection ownIdConnection);

        Task<AuthResult<object>> OnSuccessLoginAsync(string did, string publicKey);

        Task<AuthResult<object>> OnSuccessLoginByPublicKeyAsync(string publicKey);

        Task<AuthResult<object>> OnSuccessLoginByFido2Async(string fido2CredentialId, uint fido2SignCounter);

        Task<Fido2Info> FindFido2InfoAsync(string fido2CredentialId);

        Task<ConnectionRecoveryResult<object>> GetConnectionRecoveryDataAsync(string recoveryToken,
            bool includingProfile = false);
        
        Task<string> GetUserIdByEmail(string email);
        
        Task<UserSettings> GetUserSettingsAsync(string publicKey);

        Task<string> GetUserNameAsync(string did);

        Task UpgradeConnectionAsync(string publicKey, OwnIdConnection newConnection);
    }
}