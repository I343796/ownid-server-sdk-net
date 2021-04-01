using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Flow.Adapters;
using OwnID.Web.Extensibility;

namespace OwnID.Web.Features
{
    public class UserHandlerFeature : IFeature
    {
        private Action<IServiceCollection> _applyServicesAction;

        public void ApplyServices(IServiceCollection services)
        {
            _applyServicesAction(services);
        }

        public IFeature FillEmptyWithOptional()
        {
            return this;
        }

        public void Validate()
        {
            if (_applyServicesAction == null)
                throw new InvalidOperationException("UserHandler can not be null");
        }

        public UserHandlerFeature UseHandler<TProfile, THandler>() where THandler : class, IUserHandler<TProfile>
            where TProfile : class
        {
            _applyServicesAction = services =>
            {
                services.TryAddTransient<IUserHandler<TProfile>, THandler>();
                services.TryAddTransient<IUserHandlerAdapter, UserHandlerAdapter<TProfile>>();
            };

            return this;
        }
    }
}