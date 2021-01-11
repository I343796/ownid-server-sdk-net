using System;
using System.Threading.Tasks;
using OwnID.Cryptography;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensions;
using OwnID.Services;

namespace OwnID.Commands.Recovery
{
    public class SaveRecoveredAccountConnectionCommand
    {
        private readonly ICacheItemRepository _cacheItemRepository;
        private readonly IOwnIdCoreConfiguration _coreConfiguration;
        private readonly IJwtService _jwtService;
        private readonly IAccountRecoveryHandler _recoveryHandler;

        public SaveRecoveredAccountConnectionCommand(ICacheItemRepository cacheItemRepository, IJwtService jwtService,
            IAccountRecoveryHandler recoveryHandler,
            IOwnIdCoreConfiguration coreConfiguration)
        {
            _cacheItemRepository = cacheItemRepository;
            _jwtService = jwtService;
            _recoveryHandler = recoveryHandler;
            _coreConfiguration = coreConfiguration;
        }

        public async Task<CacheItem> ExecuteAsync(JwtContainer jwtContainer, CacheItem relatedItem)
        {
            var userData = _jwtService.GetDataFromJwt<UserProfileData>(jwtContainer.Jwt).Data;

            await _recoveryHandler.RemoveConnectionsAsync(userData.PublicKey);

            // TODO: remove? classic flow
            if (!_coreConfiguration.OverwriteFields)
                userData.Profile = null;

            // TODO: code duplication
            var recoveryToken = !string.IsNullOrEmpty(userData.RecoveryData) ? Guid.NewGuid().ToString("N") : null;

            await _recoveryHandler.OnRecoverAsync(userData.DID, new OwnIdConnection
            {
                PublicKey = userData.PublicKey,
                RecoveryToken = recoveryToken,
                RecoveryData = userData.RecoveryData
            });

            return await _cacheItemRepository.UpdateAsync(relatedItem.Context, item =>
            {
                item.RecoveryToken = recoveryToken;
                item.RecoveryData = userData.RecoveryData;
                item.FinishFlow(relatedItem.DID, userData.PublicKey);
            });
        }
    }
}