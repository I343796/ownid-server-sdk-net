using System;
using System.Net.Http;
using System.Threading.Tasks;
using OwnID.Commands;
using OwnID.Cryptography;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Extensibility.Providers;
using OwnID.Flow.Adapters;
using OwnID.Flow.Interfaces;
using OwnID.Flow.ResultActions;
using OwnID.Services;

namespace OwnID.Flow.TransitionHandlers.Partial
{
    public class InstantAuthorizeBaseTransitionHandler : BaseTransitionHandler<TransitionInput<JwtContainer>>
    {
        private readonly ICacheItemRepository _cacheItemRepository;
        private readonly IOwnIdCoreConfiguration _configuration;
        private readonly ICookieService _cookieService;
        private readonly IJwtService _jwtService;
        private readonly SavePartialConnectionCommand _savePartialConnectionCommand;
        private readonly IUserHandlerAdapter _userHandlerAdapter;

        public InstantAuthorizeBaseTransitionHandler(IJwtComposer jwtComposer, StopFlowCommand stopFlowCommand,
            IUrlProvider urlProvider, SavePartialConnectionCommand savePartialConnectionCommand,
            ICookieService cookieService, IJwtService jwtService, IUserHandlerAdapter userHandlerAdapter,
            IOwnIdCoreConfiguration configuration, ICacheItemRepository cacheItemRepository) : base(jwtComposer,
            stopFlowCommand, urlProvider)
        {
            _savePartialConnectionCommand = savePartialConnectionCommand;
            _cookieService = cookieService;
            _jwtService = jwtService;
            _userHandlerAdapter = userHandlerAdapter;
            _configuration = configuration;
            _cacheItemRepository = cacheItemRepository;
        }

        public override StepType StepType => StepType.InstantAuthorize;


        public override FrontendBehavior GetOwnReference(string context, ChallengeType challengeType)
        {
            return new(StepType, challengeType,
                new CallAction(UrlProvider.GetChallengeUrl(context, challengeType, "/partial")));
        }

        protected override void Validate(TransitionInput<JwtContainer> input, CacheItem relatedItem)
        {
            if (!relatedItem.IsValidForAuthorize)
                throw new CommandValidationException(
                    "Cache item should be not be Finished with PARTIAL Login or Register challenge type. "
                    + $"Actual Status={relatedItem.Status.ToString()} ChallengeType={relatedItem.ChallengeType.ToString()}");
        }

        protected override async Task<ITransitionResult> ExecuteInternalAsync(TransitionInput<JwtContainer> input,
            CacheItem relatedItem)
        {
            var userData = _jwtService.GetDataFromJwt<UserIdentitiesData>(input.Data.Jwt).Data;

            // Process registration
            if (relatedItem.ChallengeType == ChallengeType.Register)
                return await FinishAuthProcessAsync(userData, relatedItem, input);

            // Check if user settings override global one
            var settings = await _userHandlerAdapter.GetUserSettingsAsync(userData.PublicKey);
            if (!_configuration.TFAEnabled && (settings?.EnforceTFA ?? false)
                                          && userData.AuthType == ConnectionAuthType.Basic)
                return await SwitchConnectionAuthTypeAsync(relatedItem, input, userData.SupportsFido2,
                    userData.PublicKey);

            return await FinishAuthProcessAsync(userData, relatedItem, input);
        }

        private async Task<ITransitionResult> SwitchConnectionAuthTypeAsync(CacheItem relatedItem,
            TransitionInput<JwtContainer> input, bool supportsFido2, string publicKey)
        {
            relatedItem.NewAuthType
                = supportsFido2 && _configuration.Fido2.IsEnabled
                    ? ConnectionAuthType.Fido2
                    : ConnectionAuthType.Passcode;

            var composeInfo = new BaseJwtComposeInfo(input)
            {
                EncKey = relatedItem.EncKey,
                EncVector = relatedItem.EncVector,
                CookiesSet = true
            };

            switch (relatedItem.NewAuthType)
            {
                case ConnectionAuthType.Passcode:
                    composeInfo.Behavior = new FrontendBehavior(StepType.EnterPasscode, relatedItem.ChallengeType,
                        GetNextBehaviorFunc(input, relatedItem))
                    {
                        AlternativeBehavior = new FrontendBehavior(StepType.ResetPasscode, relatedItem.ChallengeType,
                            new CallAction(UrlProvider.GetResetPasscodeUrl(relatedItem.Context),
                                HttpMethod.Delete.ToString()))
                    };
                    break;
                case ConnectionAuthType.Fido2:
                {
                    await _cacheItemRepository.UpdateAsync(relatedItem.Context, item => item.OldPublicKey = publicKey);

                    var fido2Url = UrlProvider.GetFido2Url(relatedItem.Context, relatedItem.RequestToken,
                        input.CultureInfo?.Name);
                    composeInfo.Behavior = FrontendBehavior.CreateRedirect(fido2Url);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            var jwt = JwtComposer.GenerateBaseStepJwt(composeInfo);
            return new StateResult(jwt, _cookieService.CreateAuthCookies(relatedItem));
        }

        private async Task<ITransitionResult> FinishAuthProcessAsync(UserIdentitiesData userData, CacheItem relatedItem,
            TransitionInput<JwtContainer> input)
        {
            await _savePartialConnectionCommand.ExecuteAsync(userData, relatedItem);

            var composeInfo = new BaseJwtComposeInfo(input)
            {
                Behavior = FrontendBehavior.CreateSuccessFinish(relatedItem.ChallengeType),
                CookiesSet = true
            };

            var jwt = JwtComposer.GenerateFinalStepJwt(composeInfo);
            return new StateResult(jwt, _cookieService.CreateAuthCookies(relatedItem));
        }
    }
}