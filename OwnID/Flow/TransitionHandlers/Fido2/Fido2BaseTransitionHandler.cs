using OwnID.Commands;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Extensibility.Providers;
using OwnID.Flow.Interfaces;
using OwnID.Flow.ResultActions;
using OwnID.Services;

namespace OwnID.Flow.TransitionHandlers.Fido2
{
    public abstract class Fido2BaseTransitionHandler : BaseTransitionHandler<TransitionInput<string>>
    {
        private readonly ICookieService _cookieService;

        protected Fido2BaseTransitionHandler(IJwtComposer jwtComposer, StopFlowCommand stopFlowCommand,
            IUrlProvider urlProvider, ICookieService cookieService) : base(jwtComposer, stopFlowCommand, urlProvider)
        {
            _cookieService = cookieService;
        }

        public override FrontendBehavior GetOwnReference(string context, ChallengeType challengeType)
        {
            return new(StepType, challengeType,
                new CallAction(UrlProvider.GetChallengeUrl(context, challengeType, "/fido2")));
        }

        protected override void Validate(TransitionInput<string> input, CacheItem relatedItem)
        {
            if (relatedItem.FlowType != FlowType.Fido2Login && relatedItem.FlowType != FlowType.Fido2Register
                                                            && relatedItem.FlowType != FlowType.Fido2LinkWithPin
                                                            && relatedItem.FlowType != FlowType.Fido2RecoverWithPin
                                                            && relatedItem.FlowType != FlowType.PartialAuthorize)
                throw new CommandValidationException(
                    $"Can not set Fido2 information for the flow not related to Fido2. Current flow: {relatedItem.FlowType} Context: '{relatedItem.Context}'");

            if (relatedItem.Status != CacheItemStatus.Initiated && relatedItem.Status != CacheItemStatus.Started
                                                                && relatedItem.Status != CacheItemStatus.Approved)
                throw new CommandValidationException(
                    $"Incorrect status={relatedItem.Status.ToString()} for setting public key for context '{relatedItem.Context}'");
        }

        protected StateResult GenerateResult(TransitionInput<string> input, CacheItem relatedItem)
        {
            var composeInfo = new BaseJwtComposeInfo(input)
            {
                Behavior = GetNextBehaviorFunc(input, relatedItem),
                CookiesSet = true
            };

            var jwt = JwtComposer.GenerateBaseStepJwt(composeInfo, relatedItem.DID);
            //TODO: add remove other cookies
            return new StateResult(jwt, _cookieService.CreateAuthCookies(relatedItem));
        }
    }
}