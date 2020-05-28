using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnIdSdk.NetCore3.Web.Extensibility.Abstractions;
using OwnIdSdk.NetCore3.Web.FlowEntries;

namespace OwnIdSdk.NetCore3.Web.Features
{
    public class UserHandlerFeature : IFeatureConfiguration
    {
        private Action<IServiceCollection> _applyServicesAction;

        public UserHandlerFeature UseHandler<TProfile, THandler>() where THandler : class, IUserHandler<TProfile> where TProfile : class
        {
            _applyServicesAction = services =>
            {
                services.TryAddTransient<IUserHandler<TProfile>, THandler>();
                services.TryAddTransient<IUserHandlerAdapter, UserHandlerAdapter<TProfile>>();
            };
            
            return this;
        }

        public void ApplyServices(IServiceCollection services)
        {
            _applyServicesAction(services);
        }

        public IFeatureConfiguration FillEmptyWithOptional()
        {
            return this;
        }

        public void Validate()
        {
            if(_applyServicesAction == null)
                throw new InvalidOperationException($"UserHandler can not be null");
        }
    }
}