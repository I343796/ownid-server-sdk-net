using System.Threading.Tasks;
using OwnID.Commands;
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
    public class LinkBaseTransitionHandler : BaseTransitionHandler<TransitionInput<JwtContainer>>
    {
        private readonly ICookieService _cookieService;
        private readonly LinkAccountCommand _linkAccountCommand;

        public override StepType StepType => StepType.Link;

        public LinkBaseTransitionHandler(IJwtComposer jwtComposer, StopFlowCommand stopFlowCommand,
            IUrlProvider urlProvider, LinkAccountCommand linkAccountCommand, ICookieService cookieService) : base(
            jwtComposer, stopFlowCommand, urlProvider)
        {
            _linkAccountCommand = linkAccountCommand;
            _cookieService = cookieService;
        }

        public override FrontendBehavior GetOwnReference(string context, ChallengeType challengeType)
        {
            return new(StepType, challengeType, new CallAction(UrlProvider.GetChallengeUrl(context, challengeType)));
        }

        protected override void Validate(TransitionInput<JwtContainer> input, CacheItem relatedItem)
        {
            if (!relatedItem.IsValidForLink)
                throw new CommandValidationException(
                    "Cache item should be not Finished with Link challenge type. " +
                    $"Actual Status={relatedItem.Status.ToString()} ChallengeType={relatedItem.ChallengeType}");
        }

        protected override async Task<ITransitionResult> ExecuteInternalAsync(TransitionInput<JwtContainer> input,
            CacheItem relatedItem)
        {
            relatedItem = await _linkAccountCommand.ExecuteAsync(input.Data, relatedItem);

            var composeInfo = new BaseJwtComposeInfo(input)
            {
                Behavior = GetNextBehaviorFunc(input, relatedItem),
                CookiesSet = true
            };

            // TODO: change to generic step generation
            var jwt = JwtComposer.GenerateFinalStepJwt(composeInfo);
            return new StateResult(jwt, _cookieService.CreateAuthCookies(relatedItem));
        }
    }
}