using System.Threading.Tasks;
using OwnIdSdk.NetCore3.Extensibility.Flow.Contracts;

namespace OwnIdSdk.NetCore3.Extensibility.Flow.Abstractions
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
        Task UpdateProfileAsync(IUserProfileFormContext<TProfile> context);

        /// <summary>
        ///     Will be called whenever a user waits for authorization credentials on success login. Data passed to
        ///     <see cref="LoginResult{T}" /> will be sent to OwnId UI SDK and passed to provided in configuration callback
        /// </summary>
        /// <param name="did">User unique identifier</param>
        Task<LoginResult<object>> OnSuccessLoginAsync(string did);

        /// <summary>
        ///     Will be called whenever a user waits for authorization credentials on success login. Data passed to
        ///     <see cref="LoginResult{T}" /> will be sent to OwnId UI SDK and passed to provided in configuration callback
        /// </summary>
        /// <param name="publicKey">User public key</param>
        Task<LoginResult<object>> OnSuccessLoginByPublicKeyAsync(string publicKey);
        
        /// <summary>
        /// Will be called to check if user exists before executing login / register requests
        /// </summary>
        /// <param name="did">User unique identifier</param>
        /// <returns>True if user exists, False if not</returns>
        Task<bool> CheckUserExists(string did);
    }
}