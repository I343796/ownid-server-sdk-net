using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow.Contracts.Cookies;
using OwnID.Extensibility.Flow.Contracts.Fido2;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Providers;
using OwnID.Extensions;
using OwnID.Services;

namespace OwnID.Commands.Fido2
{
    public class Fido2RegisterCommand
    {
        private readonly IOwnIdCoreConfiguration _configuration;
        private readonly IEncodingService _encodingService;
        private readonly IFido2 _fido2;
        private readonly IIdentitiesProvider _identitiesProvider;
        protected readonly ICacheItemRepository CacheItemRepository;

        public Fido2RegisterCommand(IFido2 fido2, ICacheItemRepository cacheItemRepository,
            IOwnIdCoreConfiguration configuration, IIdentitiesProvider identitiesProvider,
            IEncodingService encodingService)
        {
            _fido2 = fido2;
            CacheItemRepository = cacheItemRepository;
            _configuration = configuration;
            _identitiesProvider = identitiesProvider;
            _encodingService = encodingService;
        }

        protected string NewUserId => _identitiesProvider.GenerateUserId();

        public async Task<CacheItem> ExecuteAsync(string fido2Payload, CacheItem relatedItem)
        {
            var request = OwnIdSerializer.Deserialize<Fido2RegisterRequest>(fido2Payload);

            if (string.IsNullOrWhiteSpace(request.AttestationObject))
                throw new CommandValidationException("Incorrect Fido2 register request: AttestationObject is missing");

            if (string.IsNullOrWhiteSpace(request.ClientDataJson))
                throw new CommandValidationException("Incorrect Fido2 register request: ClientDataJson is missing");

            var fido2Response = new AuthenticatorAttestationRawResponse
            {
                Id = _encodingService.Base64UrlDecode(NewUserId),
                RawId = _encodingService.Base64UrlDecode(NewUserId),
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAttestationRawResponse.ResponseData
                {
                    AttestationObject = _encodingService.Base64UrlDecode(request.AttestationObject),
                    ClientDataJson = _encodingService.Base64UrlDecode(request.ClientDataJson)
                }
            };

            var options = new CredentialCreateOptions
            {
                Challenge = _encodingService.ASCIIDecode(relatedItem.Context),
                Rp = new PublicKeyCredentialRpEntity(
                    _configuration.Fido2.RelyingPartyId,
                    _configuration.Fido2.RelyingPartyName,
                    null),
                User = new Fido2User
                {
                    DisplayName = _configuration.Fido2.UserDisplayName,
                    Name = _configuration.Fido2.UserName,
                    Id = _encodingService.Base64UrlDecode(NewUserId)
                }
            };

            var result = await _fido2.MakeNewCredentialAsync(fido2Response, options, args => Task.FromResult(true));

            if (result == null)
                throw new InternalLogicException("Cannot verify fido2 register request");

            var publicKey = _encodingService.Base64UrlEncode(result.Result.PublicKey);

            return await ProcessFido2RegisterResponseAsync(relatedItem, publicKey, result.Result.Counter,
                _encodingService.Base64UrlEncode(result.Result.CredentialId));
        }

        protected virtual async Task<CacheItem> ProcessFido2RegisterResponseAsync(CacheItem relatedItem,
            string publicKey,
            uint signatureCounter, string credentialId)
        {
            return await CacheItemRepository.UpdateAsync(relatedItem.Context, item =>
            {
                item.FinishFlow(relatedItem.DID, publicKey);
                item.Fido2SignatureCounter = signatureCounter;
                item.Fido2CredentialId = credentialId;
                item.AuthCookieType = CookieType.Fido2;
            });
        }
    }
}