using System;
using System.Threading.Tasks;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensions;
using OwnID.Flow.Adapters;
using OwnID.Services;

namespace OwnID.Commands
{
    public class SavePartialConnectionCommand
    {
        private readonly ICacheItemRepository _cacheItemRepository;
        private readonly IOwnIdCoreConfiguration _configuration;
        private readonly IUserHandlerAdapter _userHandlerAdapter;

        public SavePartialConnectionCommand(ICacheItemRepository cacheItemRepository,
            IUserHandlerAdapter userHandlerAdapter, IOwnIdCoreConfiguration configuration)
        {
            _cacheItemRepository = cacheItemRepository;
            _userHandlerAdapter = userHandlerAdapter;
            _configuration = configuration;
        }

        public async Task ExecuteAsync(UserIdentitiesData input, CacheItem relatedItem)
        {
            var recoveryToken = !string.IsNullOrEmpty(input.RecoveryData) ? Guid.NewGuid().ToString("N") : null;

            if (!_configuration.LoginOnlyEnabled)
            {
                // Credentials created at web app
                if (input.ActualChallengeType.HasValue && input.ActualChallengeType != relatedItem.ChallengeType)
                {
                    relatedItem.ChallengeType = input.ActualChallengeType.Value;
                }
                // check if users exists or not. If not - then this is LinkOnLogin
                else if (relatedItem.ChallengeType == ChallengeType.Login)
                {
                    var isUserExists = await _userHandlerAdapter.IsUserExistsAsync(input.PublicKey);
                    if (!isUserExists)
                        relatedItem.ChallengeType = ChallengeType.LinkOnLogin;
                }
            }
            else
            {
                var userExists = await _userHandlerAdapter.IsUserExistsAsync(input.PublicKey);
                relatedItem.ChallengeType = userExists ? ChallengeType.Login : ChallengeType.Register;
            }

            await _cacheItemRepository.UpdateAsync(relatedItem.Context, item =>
            {
                item.ChallengeType = relatedItem.ChallengeType;
                item.AuthCookieType = relatedItem.AuthCookieType;
                item.RecoveryToken = recoveryToken;
                item.RecoveryData = input.RecoveryData;
                item.FinishFlow(input.DID, input.PublicKey);
            });
        }
    }
}