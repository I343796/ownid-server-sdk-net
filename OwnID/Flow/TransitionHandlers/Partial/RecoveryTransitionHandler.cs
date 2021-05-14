using System.Threading.Tasks;
using OwnID.Commands;
using OwnID.Commands.Recovery;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Extensibility.Providers;
using OwnID.Flow.Interfaces;
using OwnID.Flow.ResultActions;
using OwnID.Services;

namespace OwnID.Flow.TransitionHandlers.Partial
{
    public class RecoveryTransitionHandler : BaseTransitionHandler<TransitionInput<JwtContainer>>
    {
        private readonly ICookieService _cookieService;
        private readonly SaveRecoveredAccountConnectionCommand _saveRecoveredAccountConnectionCommand;

        public override StepType StepType => StepType.Recover;

        public RecoveryTransitionHandler(IJwtComposer jwtComposer, StopFlowCommand stopFlowCommand,
            IUrlProvider urlProvider, SaveRecoveredAccountConnectionCommand saveRecoveredAccountConnectionCommand,
            ICookieService cookieService) : base(jwtComposer, stopFlowCommand, urlProvider)
        {
            _saveRecoveredAccountConnectionCommand = saveRecoveredAccountConnectionCommand;
            _cookieService = cookieService;
        }

        public override FrontendBehavior GetOwnReference(string context, ChallengeType challengeType)
        {
            return new(StepType, challengeType, new CallAction(UrlProvider.GetChallengeUrl(context, challengeType)));
        }

        protected override void Validate(TransitionInput<JwtContainer> input, CacheItem relatedItem)
        {
            if (!relatedItem.IsValidForRecover)
                throw new CommandValidationException(
                    "Cache item should be not Finished with Recover challenge type. " +
                    $"Actual Status={relatedItem.Status.ToString()} ChallengeType={relatedItem.ChallengeType}");

            if (string.IsNullOrEmpty(relatedItem.DID))
                throw new CommandValidationException("User DID was not recovered with RecoverAccountCommand");
        }

        protected override async Task<ITransitionResult> ExecuteInternalAsync(TransitionInput<JwtContainer> input,
            CacheItem relatedItem)
        {
            relatedItem = await _saveRecoveredAccountConnectionCommand.ExecuteAsync(input.Data, relatedItem);

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