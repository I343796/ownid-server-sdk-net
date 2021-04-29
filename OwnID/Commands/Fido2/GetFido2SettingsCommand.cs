using System.Threading.Tasks;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow.Contracts.Fido2;
using OwnID.Extensibility.Providers;
using OwnID.Services;

namespace OwnID.Commands.Fido2
{
    public class GetFido2SettingsCommand
    {
        private readonly ICacheItemRepository _cacheItemRepository;
        private readonly IOwnIdCoreConfiguration _ownIdCoreConfiguration;
        private readonly IUrlProvider _urlProvider;

        public GetFido2SettingsCommand(ICacheItemRepository cacheItemRepository, IUrlProvider urlProvider,
            IOwnIdCoreConfiguration ownIdCoreConfiguration)
        {
            _cacheItemRepository = cacheItemRepository;
            _urlProvider = urlProvider;
            _ownIdCoreConfiguration = ownIdCoreConfiguration;
        }

        public async Task<Fido2Settings> ExecuteAsync(string context, string requestToken, string locale)
        {
            var item = await _cacheItemRepository.GetAsync(context);

            if (item.RequestToken != requestToken)
                throw new CommandValidationException(
                    $"Can not provide settings for FIDO2. Wrong nonce actual '{requestToken}' expected '{item.ResponseToken}'");

            if (item.Status != CacheItemStatus.Approved && item.Status != CacheItemStatus.Initiated
                                                        && item.Status != CacheItemStatus.Started)
                throw new CommandValidationException(
                    $"Can not provide settings for FIDO2. Wrong item status {item.Status}");

            var url = _urlProvider.GetWebAppSignWithCallbackUrl(_urlProvider.GetStartFlowUrl(context), locale,
                item.RequestToken, item.ResponseToken);

            return new Fido2Settings
            {
                CredId = item.Fido2CredentialId,
                UserName = _ownIdCoreConfiguration.Fido2.UserName,
                UserDisplayName = _ownIdCoreConfiguration.Fido2.UserDisplayName,
                RelyingPartyId = _ownIdCoreConfiguration.Fido2.RelyingPartyId,
                RelyingPartyName = _ownIdCoreConfiguration.Fido2.RelyingPartyName,
                CallbackUrl = url.ToString(),
                LogLevel = ((int) _ownIdCoreConfiguration.LogLevel).ToString(),
                LogoUrl = _ownIdCoreConfiguration.LogoUrl?.ToString()
            };
        }
    }
}