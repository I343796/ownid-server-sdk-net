using System.Threading.Tasks;
using OwnIdSdk.NetCore3.Extensibility.Cache;
using OwnIdSdk.NetCore3.Extensibility.Exceptions;
using OwnIdSdk.NetCore3.Extensibility.Flow;
using OwnIdSdk.NetCore3.Extensibility.Flow.Abstractions;
using OwnIdSdk.NetCore3.Extensibility.Flow.Contracts.Approval;
using OwnIdSdk.NetCore3.Extensibility.Flow.Contracts.Jwt;
using OwnIdSdk.NetCore3.Flow.Commands.Recovery;
using OwnIdSdk.NetCore3.Flow.Interfaces;
using OwnIdSdk.NetCore3.Flow.Steps;

namespace OwnIdSdk.NetCore3.Flow.Commands.Approval
{
    public class GetApprovalStatusCommand : BaseFlowCommand
    {
        private readonly IFlowController _flowController;
        private readonly IJwtComposer _jwtComposer;
        private readonly IAccountRecoveryHandler _recoveryHandler;

        public GetApprovalStatusCommand(IJwtComposer jwtComposer, IFlowController flowController,
            IAccountRecoveryHandler recoveryHandler)
        {
            _jwtComposer = jwtComposer;
            _flowController = flowController;
            _recoveryHandler = recoveryHandler;
        }

        protected override void Validate(ICommandInput input, CacheItem relatedItem)
        {
        }

        protected override async Task<ICommandResult> ExecuteInternalAsync(ICommandInput input, CacheItem relatedItem,
            StepType currentStepType, bool isStateless)
        {
            string jwt = null;

            if (relatedItem.Status == CacheItemStatus.Approved)
            {
                BaseFlowCommand command;
                var isFido2 = false;

                switch (relatedItem.FlowType)
                {
                    case FlowType.LinkWithPin:
                        command = new GetNextStepCommand(_jwtComposer, _flowController, false);
                        break;
                    case FlowType.RecoverWithPin:
                        command = new RecoverAccountCommand(_jwtComposer, _flowController, _recoveryHandler, false);
                        break;
                    case FlowType.Fido2LinkWithPin:
                    case FlowType.Fido2RecoverWithPin:
                        command = new GetNextStepCommand(_jwtComposer, _flowController, false);
                        isFido2 = true;
                        break;
                    default:
                        throw new InternalLogicException(
                            $"Not supported FlowType for get approval status '{relatedItem.FlowType.ToString()}'");
                }

                var commandResult = await command.ExecuteAsync(input, relatedItem, currentStepType, isStateless: isFido2);

                if (!(commandResult is JwtContainer jwtContainer))
                    throw new InternalLogicException("Incorrect command result type");

                jwt = jwtContainer.Jwt;
            }
            else if (relatedItem.Status == CacheItemStatus.Declined)
            {
                var step = _flowController.GetExpectedFrontendBehavior(relatedItem, currentStepType);

                var composeInfo = new BaseJwtComposeInfo
                {
                    Context = relatedItem.Context,
                    ClientTime = input.ClientDate,
                    Behavior = step,
                    Locale = input.CultureInfo?.Name
                };

                jwt = _jwtComposer.GenerateFinalStepJwt(composeInfo);
            }

            return new GetApprovalStatusResponse(jwt, relatedItem.Status);
        }
    }
}