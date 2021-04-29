using System.Threading.Tasks;
using Fido2NetLib;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Extensibility.Providers;
using OwnID.Extensions;
using OwnID.Services;

namespace OwnID.Commands.Fido2
{
    public class Fido2RecoveryCommand : Fido2RegisterCommand
    {
        private readonly IAccountRecoveryHandler _recoveryHandler;

        public Fido2RecoveryCommand(IFido2 fido2, ICacheItemRepository cacheItemRepository,
            IOwnIdCoreConfiguration configuration, IIdentitiesProvider identitiesProvider,
            IEncodingService encodingService, IAccountRecoveryHandler recoveryHandler) : base(fido2,
            cacheItemRepository, configuration, identitiesProvider, encodingService)
        {
            _recoveryHandler = recoveryHandler;
        }

        protected override async Task<CacheItem> ProcessFido2RegisterResponseAsync(CacheItem relatedItem,
            string publicKey,
            uint signatureCounter, string credentialId)
        {
            await _recoveryHandler.ReplaceWithNewConnectionAsync(relatedItem.DID, new OwnIdConnection
            {
                PublicKey = publicKey,
                Fido2CredentialId = credentialId,
                Fido2SignatureCounter = signatureCounter.ToString(),
                AuthType = ConnectionAuthType.Fido2
            });

            return await CacheItemRepository.UpdateAsync(relatedItem.Context, item =>
            {
                item.FinishFlow(relatedItem.DID, publicKey);
                item.Fido2CredentialId = credentialId;
            });
        }
    }
}