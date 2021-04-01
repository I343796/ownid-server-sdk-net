using System;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Flow.Contracts.Jwt;
using OwnID.Extensibility.Flow.Contracts.Start;
using OwnID.Flow.ResultActions;
using OwnID.Flow.TransitionHandlers;
using OwnID.Flow.TransitionHandlers.Partial;

namespace OwnID.Flow.Setups.Partial
{
    public class PartialAuthorizeFlow : BasePartialFlow
    {
        public PartialAuthorizeFlow(IServiceProvider serviceProvider, IOwnIdCoreConfiguration ownIdCoreConfiguration) :
            base(serviceProvider, FlowType.PartialAuthorize, ownIdCoreConfiguration)
        {
            // 1.Starting 2.AcceptStart
            AddStartingTransitions(GetOnStartAcceptBehavior);

            // 3. (optional) ConnectionRestore
            AddHandler<ConnectionRestoreBaseTransitionHandler, TransitionInput>((_, item) =>
                GetOnRecoveryConnectionPassedBehavior(item));

            if (!ownIdCoreConfiguration.LoginOnlyEnabled)
                // 4. (optional) CheckUserExistence
                AddHandler<CheckUserExistenceBaseTransitionHandler, TransitionInput<UserIdentification>>((_, item) =>
                    GetOnInstantAuthorizeBehavior(item));
            else
                // 4. (optional) CheckUserExistence
                AddHandler<CheckUserExistenceBaseTransitionHandler, TransitionInput<UserIdentification>>((_, item) =>
                    GetOnInstantAuthorizeBehavior(item));

            // 5. InstantAuthorize
            AddHandler<InstantAuthorizeBaseTransitionHandler, TransitionInput<JwtContainer>>((_, item) =>
                GetOnSwitchAuthType(item));

            // 6. (optional) upgrade to passcode
            AddHandler<UpgradeToPasscodeTransitionHandler, TransitionInput<JwtContainer>>(OnSuccess);

            // 6. (optional) upgrade to fido
            AddHandler<UpgradeToFido2TransitionHandler, TransitionInput<string>>(OnSuccess);
        }

        private FrontendBehavior GetOnStartAcceptBehavior(TransitionInput<AcceptStartRequest> input,
            CacheItem cacheItem)
        {
            // Recover connection if there is no such but recovery token is available
            if (!input.Data.AuthType.HasValue && !string.IsNullOrEmpty(cacheItem.RecoveryToken))
                return GetReferenceToExistingStep(StepType.InternalConnectionRecovery, cacheItem.Context,
                    cacheItem.ChallengeType);

            return GetOnRecoveryConnectionPassedBehavior(cacheItem);
        }

        private FrontendBehavior GetOnRecoveryConnectionPassedBehavior(CacheItem cacheItem)
        {
            var authorize = GetOnInstantAuthorizeBehavior(cacheItem);

            if (cacheItem.ChallengeType == ChallengeType.Register && !string.IsNullOrEmpty(cacheItem.EncKey))
            {
                var checkUserExistence = GetReferenceToExistingStep(StepType.CheckUserExistence, cacheItem.Context,
                    cacheItem.ChallengeType);
                checkUserExistence.AlternativeBehavior = authorize;
                return checkUserExistence;
            }

            return authorize;
        }

        private FrontendBehavior GetOnInstantAuthorizeBehavior(CacheItem cacheItem)
        {
            return GetReferenceToExistingStep(StepType.InstantAuthorize, cacheItem.Context, cacheItem.ChallengeType);
        }

        private FrontendBehavior GetOnSwitchAuthType(CacheItem item)
        {
            var nextStepType = item.NewAuthType switch
            {
                ConnectionAuthType.Fido2 => StepType.UpgradeToFido2,
                ConnectionAuthType.Passcode => StepType.UpgradeToPasscode,
                _ => throw new ArgumentOutOfRangeException(nameof(item), "Not supported new Auth Type")
            };

            return GetReferenceToExistingStep(nextStepType, item.Context, item.ChallengeType);
        }

        private FrontendBehavior OnSuccess<T>(TransitionInput<T> _, CacheItem item)
        {
            var challengeType = item.ChallengeType;
            if (item.ChallengeType == ChallengeType.Register && item.InitialChallengeType == ChallengeType.Login
                                                             && !CoreConfiguration.LoginOnlyEnabled)
                challengeType = ChallengeType.LinkOnLogin;

            return FrontendBehavior.CreateSuccessFinish(challengeType);
        }
    }
}