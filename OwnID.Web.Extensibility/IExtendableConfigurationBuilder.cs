using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using OwnID.Extensibility.Flow.Abstractions;

namespace OwnID.Web.Extensibility
{
    public interface IExtendableConfigurationBuilder
    {
        IServiceCollection Services { get; }

        void AddOrUpdateFeature<TFeature>([NotNull] TFeature feature) where TFeature : class, IFeature;

        void UseAccountLinking<THandler>() where THandler : class, IAccountLinkHandler;

        void UseAccountRecovery<THandler>() where THandler : class, IAccountRecoveryHandler;

        void UseUserHandlerWithCustomProfile<TProfile, THandler>() where THandler : class, IUserHandler<TProfile>
            where TProfile : class;
    }
}