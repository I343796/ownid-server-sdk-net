using System.Threading.Tasks;
using OwnID.Commands;
using OwnID.Cryptography;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Cookies;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Extensibility.Providers;
using OwnID.Flow.Adapters;
using OwnID.Flow.Interfaces;
using OwnID.Flow.ResultActions;
using OwnID.Services;

namespace OwnID.Flow.TransitionHandlers.Partial
{
    public class UpgradeToPasscodeTransitionHandler : BaseTransitionHandler<TransitionInput<JwtContainer>>
    {
        private readonly ICookieService _cookieService;
        private readonly IJwtService _jwtService;
        private readonly SavePartialConnectionCommand _savePartialConnectionCommand;
        private readonly IUserHandlerAdapter _userHandlerAdapter;

        public UpgradeToPasscodeTransitionHandler(IJwtComposer jwtComposer, StopFlowCommand stopFlowCommand,
            IUrlProvider urlProvider, SavePartialConnectionCommand savePartialConnectionCommand, IJwtService jwtService,
            ICookieService cookieService, IUserHandlerAdapter userHandlerAdapter,
            bool validateSecurityTokens = true) : base(jwtComposer,
            stopFlowCommand, urlProvider, validateSecurityTokens)
        {
            _savePartialConnectionCommand = savePartialConnectionCommand;
            _jwtService = jwtService;
            _cookieService = cookieService;
            _userHandlerAdapter = userHandlerAdapter;
        }

        public override StepType StepType => StepType.UpgradeToPasscode;

        public override FrontendBehavior GetOwnReference(string context, ChallengeType challengeType)
        {
            return new(StepType, challengeType,
                new CallAction(UrlProvider.GetSwitchAuthTypeUrl(context, ConnectionAuthType.Passcode)));
        }

        protected override void Validate(TransitionInput<JwtContainer> input, CacheItem relatedItem)
        {
        }

        protected override async Task<ITransitionResult> ExecuteInternalAsync(TransitionInput<JwtContainer> input,
            CacheItem relatedItem)
        {
            var userData = _jwtService.GetDataFromJwt<UserIdentitiesData>(input.Data.Jwt).Data;
            relatedItem.AuthCookieType = CookieType.Passcode;

            await _savePartialConnectionCommand.ExecuteAsync(userData, relatedItem);

            await _userHandlerAdapter.UpgradeConnectionAsync(userData.PublicKey, new OwnIdConnection
            {
                AuthType = ConnectionAuthType.Passcode,
                PublicKey = userData.PublicKey,
                RecoveryData = userData.RecoveryData,
                RecoveryToken = relatedItem.RecoveryToken
            });

            var composeInfo = new BaseJwtComposeInfo(input)
            {
                Behavior = GetNextBehaviorFunc(input, relatedItem),
                CookiesSet = true
            };
            
            var jwt = JwtComposer.GenerateFinalStepJwt(composeInfo);
            return new StateResult(jwt, _cookieService.CreateAuthCookies(relatedItem));
        }
    }
}