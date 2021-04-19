using System;
using System.Threading.Tasks;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Link;
using OwnID.Extensibility.Providers;
using OwnID.Services;

namespace OwnID.Commands
{
    public class CreateFlowCommand
    {
        private readonly ICacheItemRepository _cacheItemRepository;
        private readonly IOwnIdCoreConfiguration _configuration;
        private readonly IIdentitiesProvider _identitiesProvider;
        private readonly IAccountLinkHandler _linkHandler;
        private readonly bool _magicLinkEnabled;
        private readonly IUrlProvider _urlProvider;

        public CreateFlowCommand(ICacheItemRepository cacheItemRepository, IUrlProvider urlProvider,
            IIdentitiesProvider identitiesProvider, IOwnIdCoreConfiguration configuration,
            IAccountLinkHandler linkHandler = null, IMagicLinkConfiguration magicLinkConfiguration = null)
        {
            _cacheItemRepository = cacheItemRepository;
            _urlProvider = urlProvider;
            _identitiesProvider = identitiesProvider;
            _configuration = configuration;
            _linkHandler = linkHandler;
            _magicLinkEnabled = magicLinkConfiguration?.RedirectUrl != null;
        }

        public async Task<GetChallengeLinkResponse> ExecuteAsync(GenerateContextRequest request)
        {
            var challengeContext = _identitiesProvider.GenerateContext();
            var nonce = _identitiesProvider.GenerateNonce();
            // classic flow is not supported
            request.IsPartial = true;

            string did = null;
            string payload = null;
            FlowType flowType;

            switch (request.Type)
            {
                case ChallengeType.Register:
                case ChallengeType.Login:
                    flowType = !request.IsPartial ? FlowType.Authorize : FlowType.PartialAuthorize;
                    break;
                case ChallengeType.Link:
                    var state = await GetLinkState(request.Payload?.ToString());
                    if (state.ConnectedDevicesCount >= _configuration.MaximumNumberOfConnectedDevices)
                        return new GetChallengeLinkResponse(default, _urlProvider.GetWebAppConnectionsUrl().ToString(),
                            default, default, _magicLinkEnabled);

                    did = state.DID;
                    flowType = request.IsQr ? FlowType.LinkWithPin : FlowType.Link;
                    break;
                case ChallengeType.Recover:
                    payload = request.Payload.ToString();
                    flowType = request.IsQr ? FlowType.RecoverWithPin : FlowType.Recover;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await _cacheItemRepository.CreateAsync(new CacheItem
            {
                ChallengeType = request.Type,
                InitialChallengeType = request.Type,
                Nonce = nonce,
                Context = challengeContext,
                DID = did,
                Payload = payload,
                FlowType = flowType,
                IsDesktop = request.IsQr
            });

            var startFlowUrl = _urlProvider.GetStartFlowUrl(challengeContext);
            var destinationUrl = _urlProvider.GetWebAppSignWithCallbackUrl(startFlowUrl, request.Language);
            
            var result = new GetChallengeLinkResponse(challengeContext, destinationUrl.ToString(), nonce,
                _configuration.CacheExpirationTimeout, _magicLinkEnabled);
            result.Config.LogLevel = ((int) _configuration.LogLevel).ToString();

            return result;
        }

        private async Task<LinkState> GetLinkState(string payload)
        {
            if (_linkHandler == null)
                throw new NotSupportedException("Account Link feature was not enabled but was invoked");

            return await _linkHandler.GetCurrentUserLinkStateAsync(payload);
        }
    }
}