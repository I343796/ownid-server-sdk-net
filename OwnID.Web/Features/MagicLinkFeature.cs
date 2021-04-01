using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Commands.MagicLink;
using OwnID.Configuration;
using OwnID.Configuration.Validators;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Configuration.Validators;
using OwnID.Web.Attributes;
using OwnID.Web.Extensibility;

namespace OwnID.Web.Features
{
    [FeatureDependency(typeof(CoreFeature))]
    public class MagicLinkFeature : IFeature
    {
        private readonly IMagicLinkConfiguration _configuration = new MagicLinkConfiguration();

        private readonly IConfigurationValidator<IMagicLinkConfiguration> _validator =
            new MagicLinkConfigurationValidator();

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAddSingleton<SendMagicLinkCommand>();
            services.TryAddSingleton<ExchangeMagicLinkCommand>();
            services.TryAddSingleton(_configuration);
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


        public MagicLinkFeature WithConfiguration(Action<IMagicLinkConfiguration> setupAction)
        {
            setupAction(_configuration);
            return this;
        }
    }
}