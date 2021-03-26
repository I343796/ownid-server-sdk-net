using System;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Flow.ResultActions;
using OwnID.Flow.TransitionHandlers;

namespace OwnID.Flow.Setups.Partial
{
    public abstract class BasePartialFlow : BaseFlow
    {
        protected readonly IOwnIdCoreConfiguration CoreConfiguration;

        protected BasePartialFlow(IServiceProvider serviceProvider, FlowType flowType,
            IOwnIdCoreConfiguration coreConfiguration) : base(serviceProvider, flowType)
        {
            CoreConfiguration = coreConfiguration;
            AddHandler<StopFlowTransitionHandler, TransitionInput>((_, item) => new FrontendBehavior
            {
                Type = StepType.Stopped,
                ChallengeType = item.ChallengeType,
                ActionType = ActionType.Finish
            });
        }

        protected sealed override void AddStartingTransitions(
            Func<TransitionInput<AcceptStartRequest>, CacheItem, FrontendBehavior> nextStepDecider)
        {
            FrontendBehavior ModifiedDecider(TransitionInput<AcceptStartRequest> input, CacheItem item)
            {
                return TryAddFido2DisclaimerToBehavior(input, item, nextStepDecider(input, item));
            }

            base.AddStartingTransitions(ModifiedDecider);
        }

        protected void AddStartingTransitionsWithPin<TAcceptStart>(StepType nextStep)
            where TAcceptStart : AcceptStartTransitionHandler
        {
            // 1. Starting
            AddHandler<StartFlowWithPinTransitionHandler, TransitionInput<StartRequest>>((input, item) =>
                GetReferenceToExistingStep(
                    item.Status == CacheItemStatus.Approved ? StepType.AcceptStart : StepType.ApprovePin, input.Context,
                    item.ChallengeType));

            // 2. PinApprovalStatus
            AddHandler<PinApprovalStatusTransitionHandler, TransitionInput>((input, item) =>
                GetReferenceToExistingStep(StepType.AcceptStart, input.Context, item.ChallengeType));

            // 3. AcceptStart
            AddHandler<TAcceptStart, TransitionInput<AcceptStartRequest>>((input, item) =>
            {
                var next = GetReferenceToExistingStep(nextStep, item.Context, item.ChallengeType);
                return TryAddFido2DisclaimerToBehavior(input, item, next);
            });
        }

        protected void AddStartingTransitionsWithPin(StepType nextStep)
        {
            AddStartingTransitionsWithPin<AcceptStartTransitionHandler>(nextStep);
        }

        protected FrontendBehavior TryAddFido2DisclaimerToBehavior(TransitionInput<AcceptStartRequest> input,
            CacheItem relatedItem, FrontendBehavior expectedBehavior)
        {
            if (CoreConfiguration.TFAEnabled && CoreConfiguration.Fido2FallbackBehavior == Fido2FallbackBehavior.Basic
                                              && !input.Data.SupportsFido2
                                              && relatedItem.ChallengeType != ChallengeType.Login)
                return new FrontendBehavior(StepType.Fido2Disclaimer, relatedItem.ChallengeType, expectedBehavior)
                {
                    AlternativeBehavior = GetReferenceToExistingStep(StepType.Stopped, relatedItem.Context,
                        relatedItem.ChallengeType)
                };

            return expectedBehavior;
        }
    }
}