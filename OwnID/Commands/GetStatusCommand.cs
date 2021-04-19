using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OwnID.Cryptography;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Extensions;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Services;
using OwnID.Flow.Adapters;
using OwnID.Services;

namespace OwnID.Commands
{
    public class GetStatusCommand
    {
        private readonly ICacheItemRepository _cacheItemRepository;
        private readonly IJwtService _jwtService;
        private readonly IOwnIdCoreConfiguration _configuration;
        private readonly ILocalizationService _localizationService;
        private readonly IUserHandlerAdapter _userHandlerAdapter;

        public GetStatusCommand(IUserHandlerAdapter userHandlerAdapter, ICacheItemRepository cacheItemRepository,
            ILocalizationService localizationService, IJwtService jwtService, IOwnIdCoreConfiguration configuration)
        {
            _userHandlerAdapter = userHandlerAdapter;
            _cacheItemRepository = cacheItemRepository;
            _localizationService = localizationService;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<List<GetStatusResponse>> ExecuteAsync(List<GetStatusRequest> request)
        {
            var response = new List<GetStatusResponse>();

            foreach (var statusRequestItem in request)
            {
                var responseItem = await GetContextStatus(statusRequestItem);

                if (responseItem == null)
                    continue;

                if (responseItem.Status == CacheItemStatus.Finished)
                {
                    response = new List<GetStatusResponse>(1) {responseItem};
                    break;
                }

                response.Add(responseItem);
            }

            return response;
        }

        private async Task<GetStatusResponse> GetContextStatus(GetStatusRequest requestItem)
        {
            if (string.IsNullOrEmpty(requestItem.Context) || string.IsNullOrEmpty(requestItem.Nonce))
                return null;

            var cacheItem = await _cacheItemRepository.GetAsync(requestItem.Context, false);

            if (cacheItem == null || cacheItem.Nonce != requestItem.Nonce || cacheItem.Status == CacheItemStatus.Popped)
                return null;

            var result = new GetStatusResponse
            {
                Status = cacheItem.Status,
                Context = requestItem.Context
            };

            if (cacheItem.SecurityCode != null && cacheItem.Status == CacheItemStatus.WaitingForApproval)
                // TODO: refactor to entity
                result.Payload = new
                {
                    data = new
                    {
                        pin = cacheItem.SecurityCode
                    }
                };

            if (cacheItem.Status != CacheItemStatus.Finished)
                return result;

            await _cacheItemRepository.UpdateAsync(cacheItem.Context,
                item => { item.Status = CacheItemStatus.Popped; });

            if (!string.IsNullOrEmpty(cacheItem.Error))
            {
                result.Payload = new AuthResult<object>(_localizationService.GetLocalizedString(cacheItem.Error));
                return result;
            }

            var action = cacheItem.ChallengeType.ToString();

            if (cacheItem.FlowType == FlowType.Authorize)
                result.Payload = await _userHandlerAdapter.OnSuccessLoginAsync(cacheItem.DID, cacheItem.PublicKey);
            else
                switch (cacheItem.ChallengeType)
                {
                    case ChallengeType.Login
                        when cacheItem.FlowType == FlowType.Fido2Login
                             && string.IsNullOrWhiteSpace(cacheItem.Fido2CredentialId):
                    {
                        var errorMessage = _localizationService.GetLocalizedString("Error_UserNotFound");
                        result.Payload = new AuthResult<object>(errorMessage);
                        break;
                    }
                    case ChallengeType.Login
                        when !string.IsNullOrWhiteSpace(cacheItem.Fido2CredentialId)
                             && cacheItem.Fido2SignatureCounter.HasValue:
                    {
                        result.Payload = await _userHandlerAdapter.OnSuccessLoginByFido2Async(
                            cacheItem.Fido2CredentialId,
                            cacheItem.Fido2SignatureCounter.Value);
                        break;
                    }
                    case ChallengeType.Login:
                        result.Payload = await _userHandlerAdapter.OnSuccessLoginByPublicKeyAsync(cacheItem.PublicKey);
                        break;
                    case ChallengeType.LinkOnLogin:
                        action = ChallengeType.Link.ToString();
                        result.Payload = SetPartialRegisterResult(cacheItem);
                        break;
                    case ChallengeType.Register
                        when await _userHandlerAdapter.IsUserExistsAsync(cacheItem.PublicKey):
                    {
                        var errorMessage = _localizationService.GetLocalizedString("Error_PhoneAlreadyConnected");
                        result.Payload = new AuthResult<object>(errorMessage);
                        return result;
                    }
                    case ChallengeType.Register when _configuration.LoginOnlyEnabled:
                        result.Payload =
                            await _userHandlerAdapter.RegisterPartialAsync(cacheItem.DID, cacheItem.GetConnection());
                        break;
                    case ChallengeType.Register
                        when string.IsNullOrWhiteSpace(cacheItem.Fido2CredentialId):
                    {
                        result.Payload = SetPartialRegisterResult(cacheItem);
                        break;
                    }
                    case ChallengeType.Register:
                        result.Payload = new
                        {
                            data = new
                            {
                                pubKey = cacheItem.PublicKey,
                                fido2SignatureCounter = cacheItem.Fido2SignatureCounter.ToString(),
                                fido2CredentialId = cacheItem.Fido2CredentialId
                            }
                        };
                        if (cacheItem.InitialChallengeType == ChallengeType.Login
                            && cacheItem.ChallengeType == ChallengeType.Register)
                            action = ChallengeType.Link.ToString();
                        break;
                    //
                    // TODO: fix needed at web-ui-sdk to avoid error in console if data is undefined
                    //
                    case ChallengeType.Recover:
                    case ChallengeType.Link:
                        result.Payload = new
                        {
                            data = new { }
                        };
                        break;
                }

            result.Metadata = _jwtService.GenerateDataJwt(new Dictionary<string, object>
            {
                {
                    "data", new
                    {
                        action,
                        authType = cacheItem.GetAuthType()
                    }
                }
            });

            return result;
        }

        private object SetPartialRegisterResult(CacheItem cacheItem)
        {
            return new
            {
                data = new
                {
                    pubKey = cacheItem.PublicKey,
                    keyHsh = cacheItem.PublicKey.ToSha256(),
                    recoveryId = cacheItem.RecoveryToken,
                    recoveryEncData = cacheItem.RecoveryData
                }
            };
        }
    }
}