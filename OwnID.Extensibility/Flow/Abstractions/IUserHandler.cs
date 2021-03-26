using System.Threading.Tasks;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Fido2;
using OwnID.Extensibility.Flow.Contracts.Internal;

namespace OwnID.Extensibility.Flow.Abstractions
{
    /// <summary>
    ///     Describes base User handling operations
    /// </summary>
    /// <remarks>
    ///     Implement this interface to manually integrate into challenge process with custom User Profile model.
    ///     Use
    ///     <c>
    ///         IServiceCollection.AddOwnId(builder => { builder.UseUserHandlerWithCustomProfile<![CDATA[<MyHandler>]]>(); })
    ///     </c>
    ///     for this purpose
    /// </remarks>
    /// <typeparam name="TProfile">User Profile</typeparam>
    public interface IUserHandler<TProfile> where TProfile : class
    {
        /// <summary>
        ///     Will be called whenever a user provided his profile data on registration or login
        /// </summary>
        /// <param name="context">
        ///     <see cref="IUserProfileFormContext{TProfile}" /> that provides valid user information and allows
        ///     to add validation or general errors
        /// </param>
        /// <param name="recoveryToken">Recovery token</param>
        /// <param name="recoveryData">Recovery data</param>
        Task CreateProfileAsync(IUserProfileFormContext<TProfile> context, string recoveryToken = null,
            string recoveryData = null);

        /// <summary>
        ///     Will be called whenever a user provided his profile data on registration or login
        /// </summary>
        /// <param name="context">
        ///     <see cref="IUserProfileFormContext{TProfile}" /> that provides valid user information and allows
        ///     to add validation or general errors
        /// </param>
        Task UpdateProfileAsync(IUserProfileFormContext<TProfile> context);

        /// <summary>
        /// Creates user with connection
        /// </summary>
        /// <param name="did">User unique identifier</param>
        /// <param name="ownIdConnection">Connection that should be added</param>
        Task<AuthResult<object>> RegisterPartialAsync(string did, OwnIdConnection ownIdConnection);
        
        /// <summary>
        ///     Will be called whenever a user waits for authorization credentials on success login. Data passed to
        ///     <see cref="AuthResult{T}" /> will be sent to OwnId UI SDK and passed to provided in configuration callback
        /// </summary>
        /// <param name="did">User unique identifier</param>
        /// <param name="publicKey">User public key</param>
        Task<AuthResult<object>> OnSuccessLoginAsync(string did, string publicKey);

        /// <summary>
        ///     Will be called whenever a user waits for authorization credentials on success login. Data passed to
        ///     <see cref="AuthResult{T}" /> will be sent to OwnId UI SDK and passed to provided in configuration callback
        /// </summary>
        /// <param name="publicKey">User public key</param>
        Task<AuthResult<object>> OnSuccessLoginByPublicKeyAsync(string publicKey);

        /// <summary>
        ///     Will be called whenever a user waits for authorization credentials on success login. Data passed to
        ///     <see cref="AuthResult{T}" /> will be sent to OwnId UI SDK and passed to provided in configuration callback
        /// </summary>
        /// <param name="fido2CredentialId">fido2 credential id</param>
        /// <param name="fido2SignCounter">fido2 sign counter</param>
        Task<AuthResult<object>> OnSuccessLoginByFido2Async(string fido2CredentialId, uint fido2SignCounter);

        /// <summary>
        ///     Will be called to define if user with such did and public key exists.
        ///     During this method following checks should be preformed: user exists, public key exists in user data.
        /// </summary>
        /// <param name="did">User unique identifier</param>
        /// <param name="publicKey">User public key</param>
        /// <returns>Check result <see cref="IdentitiesCheckResult" /></returns>
        Task<IdentitiesCheckResult> CheckUserIdentitiesAsync(string did, string publicKey);

        /// <summary>
        ///     Checks if user with provided public key already exists
        /// </summary>
        /// <param name="publicKey">public key</param>
        /// <returns>
        ///     A task that represents the asynchronous check operation.
        ///     The task result contains true if user with provided public key exists,
        ///     otherwise false
        /// </returns>
        Task<bool> IsUserExists(string publicKey);

        /// <summary>
        ///     Checks if user with provided Fido2 credential id exists
        /// </summary>
        /// <param name="fido2CredentialId">Fido2 credential id</param>
        /// <returns>
        ///     A task that represents the asynchronous check operation.
        ///     The task result contains true if user with provided public key exists,
        ///     otherwise false
        /// </returns>
        Task<bool> IsFido2UserExists(string fido2CredentialId);

        /// <summary>
        ///     Try find Fido2 public key by fido2 user id
        /// </summary>
        /// <param name="fido2CredentialId">fido2 credential id</param>
        /// <returns>
        ///     A task that represents the asynchronous find operation.
        ///     The task result contains fido2 information if user has been found,
        ///     otherwise null
        /// </returns>
        Task<Fido2Info> FindFido2Info(string fido2CredentialId);

        /// <summary>
        ///     Gets data to recover connection in OwnID web app
        /// </summary>
        /// <param name="recoveryToken">Recovery token to search</param>
        /// <param name="includingProfile">Indicates if result should contain user profile</param>
        /// <returns>Data to recover</returns>
        Task<ConnectionRecoveryResult<TProfile>> GetConnectionRecoveryDataAsync(string recoveryToken,
            bool includingProfile = false);

        /// <summary>
        ///     Searches and returns user ID with provided email
        /// </summary>
        /// <param name="email">Email to search</param>
        /// <returns>User ID if there is a user with such email or null if not</returns>
        Task<string> GetUserIdByEmail(string email);

        /// <summary>
        ///     Gets user settings
        /// </summary>
        /// <param name="publicKey">user public key</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation.
        ///     The task result contains user settings if user with provided public key exists,
        ///     otherwise null
        /// </returns>
        Task<UserSettings> GetUserSettingsAsync(string publicKey);

        /// <summary>
        ///     Upgrades connection
        /// </summary>
        /// <param name="did"></param>
        /// <param name="newConnection"></param>
        /// <returns></returns>
        Task UpgradeConnectionAsync(string did, OwnIdConnection newConnection);

        /// <summary>
        ///     Gets user name
        /// </summary>
        /// <param name="did">User unique identifier</param>
        /// <returns>User name</returns>
        Task<string> GetUserNameAsync(string did);
    }
}