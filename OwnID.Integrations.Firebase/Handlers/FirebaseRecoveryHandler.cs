using System;
using System.Threading.Tasks;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.AccountRecovery;

namespace OwnID.Integrations.Firebase.Handlers
{
    public class FirebaseRecoveryHandler : IAccountRecoveryHandler
    {
        public Task<AccountRecoveryResult> RecoverAsync(string accountRecoveryPayload)
        {
            return Task.FromException<AccountRecoveryResult>(new NotImplementedException());
        }

        public Task OnRecoverAsync(string did, OwnIdConnection connection)
        {
            return Task.FromException(new NotImplementedException());
        }

        public Task RemoveConnectionsAsync(string publicKey)
        {
            return Task.FromException(new NotImplementedException());
        }
    }
}