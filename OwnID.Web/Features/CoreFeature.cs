using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Commands;
using OwnID.Commands.Fido2;
using OwnID.Commands.Pin;
using OwnID.Commands.Recovery;
using OwnID.Configuration;
using OwnID.Configuration.Validators;
using OwnID.Cryptography;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Configuration.Validators;
using OwnID.Extensibility.Providers;
using OwnID.Flow;
using OwnID.Flow.Interfaces;
using OwnID.Flow.Setups.Fido2;
using OwnID.Flow.Setups.Partial;
using OwnID.Flow.TransitionHandlers;
using OwnID.Flow.TransitionHandlers.Fido2;
using OwnID.Flow.TransitionHandlers.Partial;
using OwnID.Providers;
using OwnID.Services;
using OwnID.Web.Extensibility;

namespace OwnID.Web.Features
{
    public class CoreFeature : IFeature
    {
        private readonly OwnIdCoreConfiguration _configuration = new();

        private readonly IConfigurationValidator<OwnIdCoreConfiguration> _validator =
            new OwnIDCoreConfigurationValidator();

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAddSingleton(x => (IOwnIdCoreConfiguration) _configuration);

            services.TryAddSingleton<IJwtService, JwtService>();
            services.TryAddSingleton<IJwtComposer, JwtComposer>();
            services.TryAddSingleton<ICacheItemRepository, CacheItemRepository>();
            services.TryAddSingleton<IEncodingService, EncodingService>();
            services.TryAddSingleton<IUrlProvider, UrlProvider>();
            services.TryAddSingleton<IIdentitiesProvider, GuidIdentitiesProvider>();
            services.TryAddSingleton<ICookieService, CookieService>();

            // TODO: add interface to find all commands by it with reflection and inject
            services.TryAddSingleton<AddConnectionCommand>();
            services.TryAddSingleton<CheckUserExistenceCommand>();
            services.TryAddSingleton<CreateFlowCommand>();
            services.TryAddSingleton<GetStatusCommand>();
            services.TryAddSingleton<InternalConnectionRecoveryCommand>();
            services.TryAddSingleton<LinkAccountCommand>();
            services.TryAddSingleton<SavePartialConnectionCommand>();
            services.TryAddSingleton<SetNewEncryptionTokenCommand>();
            services.TryAddSingleton<StartFlowCommand>();
            services.TryAddSingleton<StopFlowCommand>();
            services.TryAddSingleton<TrySwitchToFido2FlowCommand>();
            services.TryAddSingleton<SetPinCommand>();
            services.TryAddSingleton<ApproveActionCommand>();
            services.TryAddSingleton<RecoverAccountCommand>();
            services.TryAddSingleton<SaveRecoveredAccountConnectionCommand>();
            services.TryAddSingleton<VerifyFido2CredentialIdCommand>();
            services.TryAddSingleton<GetFido2SettingsCommand>();

            services.TryAddTransient<AcceptStartTransitionHandler>();
            services.TryAddTransient<CheckUserExistenceBaseTransitionHandler>();
            services.TryAddTransient<UpgradeToFido2TransitionHandler>();
            services.TryAddTransient<UpgradeToPasscodeTransitionHandler>();
            services.TryAddTransient<PinApprovalStatusTransitionHandler>();
            services.TryAddTransient<StartFlowTransitionHandler>();
            services.TryAddTransient<StartFlowWithPinTransitionHandler>();

            services.TryAddTransient<ConnectionRestoreBaseTransitionHandler>();
            services.TryAddTransient<InstantAuthorizeBaseTransitionHandler>();
            services.TryAddTransient<LinkBaseTransitionHandler>();
            services.TryAddTransient<RecoverAcceptStartTransitionHandler>();
            services.TryAddTransient<RecoveryTransitionHandler>();
            services.TryAddTransient<StopFlowTransitionHandler>();

            services.TryAddSingleton<LinkFlow>();
            services.TryAddSingleton<LinkWithPinFlow>();
            services.TryAddSingleton<PartialAuthorizeFlow>();
            services.TryAddSingleton<RecoveryFlow>();
            services.TryAddSingleton<RecoveryWithPinFlow>();
            services.TryAddSingleton<IFlowRunner, FlowRunner>();

            services.TryAddSingleton<Fido2RegisterCommand>();
            services.TryAddSingleton<Fido2LoginCommand>();
            services.TryAddSingleton<Fido2LinkCommand>();
            services.TryAddSingleton<Fido2RecoveryCommand>();
            services.TryAddSingleton<Fido2UpgradeConnectionCommand>();

            services.TryAddTransient<Fido2LinkTransitionHandler>();
            services.TryAddTransient<Fido2LoginTransitionHandler>();
            services.TryAddTransient<Fido2RecoveryTransitionHandler>();
            services.TryAddTransient<Fido2RegisterTransitionHandler>();

            services.TryAddSingleton<Fido2LinkFlow>();
            services.TryAddSingleton<Fido2RecoveryFlow>();
            services.TryAddSingleton<Fido2LoginFlow>();
            services.TryAddSingleton<Fido2RegisterFlow>();

            services.AddFido2(fido2Config =>
            {
                if (_configuration.Fido2.IsEnabled)
                    fido2Config.Origin = _configuration.Fido2.Origin.ToString().TrimEnd('/').Trim();
            });
        }

        public IFeature FillEmptyWithOptional()
        {
            _validator.FillEmptyWithOptional(_configuration);

            return this;
        }

        public void Validate()
        {
            _validator.Validate(_configuration);
        }

        public CoreFeature WithConfiguration(Action<IOwnIdCoreConfiguration> setupAction)
        {
            setupAction(_configuration);
            return this;
        }

        public CoreFeature WithKeys(string publicKeyPath, string privateKeyPath)
        {
            using var publicKeyReader = File.OpenText(publicKeyPath);
            using var privateKeyReader = File.OpenText(privateKeyPath);
            WithKeys(publicKeyReader, privateKeyReader);
            return this;
        }

        public CoreFeature WithKeys(TextReader publicKeyReader, TextReader privateKeyReader)
        {
            _configuration.JwtSignCredentials = RsaHelper.LoadKeys(publicKeyReader, privateKeyReader);
            return this;
        }

        public CoreFeature WithKeys(RSA rsa)
        {
            _configuration.JwtSignCredentials = rsa;
            return this;
        }
    }
}