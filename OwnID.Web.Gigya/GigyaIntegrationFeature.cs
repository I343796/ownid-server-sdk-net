using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Extensibility.Configuration.Validators;
using OwnID.Web.Extensibility;
using OwnID.Web.Gigya.ApiClient;
using OwnID.Web.Gigya.Configuration;

namespace OwnID.Web.Gigya
{
    public class GigyaIntegrationFeature : IFeature
    {
        private readonly IGigyaConfiguration _configuration = new GigyaConfiguration();
        private readonly IConfigurationValidator<IGigyaConfiguration> _validator = new GigyaConfigurationValidator();
        
        private Action<IServiceCollection> _setupServicesAction;

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAddSingleton(_configuration);
            _setupServicesAction?.Invoke(services);
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

        public GigyaIntegrationFeature WithConfig<TProfile>(Action<IGigyaConfiguration> configAction)
            where TProfile : class, IGigyaUserProfile
        {
            configAction(_configuration);
            _setupServicesAction = collection => collection.TryAddSingleton<GigyaRestApiClient<TProfile>>();
            return this;
        }
    }
}