using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Web.Extensibility;

namespace OwnID.Integrations.IAS
{
    class IASIntegrationFeature : IFeature
    {
        private readonly IASConfiguration _configuration;
        private Action<IServiceCollection> _setupServicesAction;
        
        public IASIntegrationFeature()
        {
            _configuration = new IASConfiguration();
        }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.TryAddSingleton(_configuration);
            _setupServicesAction?.Invoke(services);
        }

        public IFeature FillEmptyWithOptional()
        {
            return this;
        }

        public void Validate()
        {
            //validate configuration
        }

        public IASIntegrationFeature WithConfig(Action<IASConfiguration> configAction)
        {
            configAction(_configuration);
            return this;
        }
    }
}
