using System.Threading.Tasks;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.AccountRecovery;

namespace OwnID.Extensibility.Flow.Abstractions
{
    /// <summary>
    ///     Define a interface for supporting account recovery process
    /// </summary>
    public interface IAccountRecoveryHandler
    {
        /// <summary>
        ///     Recover account access
        /// </summary>
        /// <param name="accountRecoveryPayload">account recovery payload</param>
        /// <returns>
        ///     A task that represents the asynchronous recover operation.
        ///     The task result contains <see cref="AccountRecoveryResult" />
        /// </returns>
        Task<AccountRecoveryResult> RecoverAsync(string accountRecoveryPayload);

        /// <summary>
        ///     Replace all users connections with new one after successful recovery
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous recover operation.
        /// </returns>
        Task ReplaceWithNewConnectionAsync(string did, OwnIdConnection connection);
    }
}