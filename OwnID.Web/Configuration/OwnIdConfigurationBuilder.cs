using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OwnID.Configuration;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow.Abstractions;
using OwnID.Web.Extensibility;
using OwnID.Web.Features;

namespace OwnID.Web.Configuration
{
    /// <summary>
    ///     Configuration composition helper
    /// </summary>
    public class OwnIdConfigurationBuilder : IExtendableConfigurationBuilder
    {
        public OwnIdConfigurationBuilder(IServiceCollection services) : this(
            new OwnIdConfiguration(new Dictionary<Type, IFeature>()))
        {
            Services = services;
            AddOrUpdateFeature(new CoreFeature().FillEmptyWithOptional());
            AddOrUpdateFeature(new LocalizationFeature().FillEmptyWithOptional());
        }

        public OwnIdConfigurationBuilder(OwnIdConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        ///     OwnId SDK features configuration
        /// </summary>
        public OwnIdConfiguration Configuration { get; private set; }

        /// <summary>
        ///     Provided Service Collection
        /// </summary>
        public IServiceCollection Services { get; }

        public void UseAccountLinking<THandler>() where THandler : class, IAccountLinkHandler
        {
            WithFeature<AccountLinkFeature>(x => x.UseAccountLinking<THandler>());
        }

        public void UseAccountRecovery<THandler>()
            where THandler : class, IAccountRecoveryHandler
        {
            WithFeature<AccountRecoveryFeature>(x => x.UseAccountRecovery<THandler>());
        }

        /// <summary>
        ///     Sets <see cref="IUserHandler{TProfile}" /> for user authorization and profile update
        /// </summary>
        /// <typeparam name="TProfile">User Profile</typeparam>
        /// <typeparam name="THandler">Custom implementation of <see cref="IUserHandler{TProfile}" /></typeparam>
        public void UseUserHandlerWithCustomProfile<TProfile, THandler>()
            where THandler : class, IUserHandler<TProfile> where TProfile : class
        {
            var handlerFeature = Configuration.FindFeature<UserHandlerFeature>() ?? new UserHandlerFeature();
            var builder = WithFeature<CoreFeature>(x =>
                x.WithConfiguration(b =>
                {
                    ((OwnIdCoreConfiguration) b).SetProfileModel<TProfile>();
                    b.ProfileConfiguration.BuildMetadata();
                })
            );
            builder.AddOrUpdateFeature(handlerFeature.UseHandler<TProfile, THandler>());
        }

        /// <summary>
        ///     Allows to add features for further extensibility
        /// </summary>
        /// <param name="feature"><see cref="IFeature" /> implementation</param>
        /// <typeparam name="TFeature">Feature to be added</typeparam>
        public void AddOrUpdateFeature<TFeature>([NotNull] TFeature feature)
            where TFeature : class, IFeature
        {
            Configuration = Configuration.WithFeature(feature);
        }

        /// <summary>
        ///     Allows to change Core Configuration that is necessary for start
        /// </summary>
        public void WithBaseSettings([NotNull] Action<IOwnIdCoreConfiguration> modifyAction)
        {
            WithFeature<CoreFeature>(x => x.WithConfiguration(modifyAction));
        }

        /// <summary>
        ///     Sets RSA keys for JWT signing and site / organization identification from files
        /// </summary>
        /// <param name="publicKeyPath">Path to public key file</param>
        /// <param name="privateKeyPath">Path to private key file</param>
        public void SetKeysFromFiles([NotNull] string publicKeyPath, [NotNull] string privateKeyPath)
        {
            WithFeature<CoreFeature>(x => x.WithKeys(publicKeyPath, privateKeyPath));
        }

        /// <summary>
        ///     Sets RSA keys for JWT signing and site / organization identification from strings
        /// </summary>
        /// <param name="publicKey">Path to public key file</param>
        /// <param name="privateKey">Path to private key file</param>
        public void SetKeys([NotNull] string publicKey, [NotNull] string privateKey)
        {
            WithFeature<CoreFeature>(x =>
            {
                using var publicKeyReader = new StringReader(publicKey);
                using var privateKeyReader = new StringReader(privateKey);
                return x.WithKeys(publicKeyReader, privateKeyReader);
            });
        }

        /// <summary>
        ///     Sets RSA keys directly as <see cref="RSA" /> object for JWT signing and site / organization identification
        /// </summary>
        /// <param name="rsa"></param>
        public void SetKeys([NotNull] RSA rsa)
        {
            WithFeature<CoreFeature>(x => x.WithKeys(rsa));
        }

        /// <summary>
        ///     Set custom localization resource to <see cref="LocalizationService" />
        /// </summary>
        public void SetLocalizationResource([NotNull] Type resourceType, [NotNull] string resourceName)
        {
            WithFeature<LocalizationFeature>(x => x.WithResource(resourceType, resourceName));
        }

        /// <summary>
        ///     Set custom <see cref="IStringLocalizer{T}" /> to <see cref="LocalizationService" />
        /// </summary>
        /// <typeparam name="TLocalizer">Custom <see cref="IStringLocalizer{T}" /> localizer</typeparam>
        public void SetStringLocalizer<TLocalizer>() where TLocalizer : IStringLocalizer
        {
            WithFeature<LocalizationFeature>(x => x.WithStringLocalizer<TLocalizer>());
        }

        /// <summary>
        ///     Defines usage of any <see cref="ICacheStore" /> implementation to store technical authorization data
        /// </summary>
        /// <param name="serviceLifetime">Life time of <typeparamref name="TStore" /> instance</param>
        /// <typeparam name="TStore"><see cref="ICacheStore" /> implementation</typeparam>
        public void UseCacheStore<TStore>(ServiceLifetime serviceLifetime) where TStore : class, ICacheStore
        {
            WithFeature<CacheStoreFeature>(x => x.UseStore<TStore>(serviceLifetime));
        }

        /// <summary>
        ///     Defines usage of any <see cref="ICacheStore" /> implementation to store technical authorization data
        /// </summary>
        /// <param name="cacheStoreFactory">cache store factory</param>
        /// <param name="serviceLifetime">cache store lifetime</param>
        public void UseCacheStore<TStore>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TStore> cacheStoreFactory) where TStore : class, ICacheStore
        {
            WithFeature<CacheStoreFeature>(x => x.UseStore(serviceLifetime, cacheStoreFactory));
        }

        /// <summary>
        ///     Defines usage of In Memory Cache Store
        /// </summary>
        public void UseInMemoryCacheStore()
        {
            WithFeature<CacheStoreFeature>(x => x.UseInMemoryStore());
        }

        /// <summary>
        ///     Defines usage of Default ASP.NET Core Web Cache Store
        /// </summary>
        /// <remarks>
        ///     Used by default
        /// </remarks>
        public void UseWebCacheStore()
        {
            WithFeature<CacheStoreFeature>(x => x.UseWebCacheStore());
        }

        /// <summary>
        ///     Enables sending mail with SMTP
        /// </summary>
        public void UseSmtp([NotNull] Action<ISmtpConfiguration> modifyAction)
        {
            WithFeature<EmailFeature>(x => x.WithConfiguration(modifyAction));
        }
        
        /// <summary>
        ///     Allows to use Magic Link feature
        /// </summary>
        /// <remarks>
        ///     SMTP is required
        /// </remarks>
        public void UseMagicLink([NotNull] Action<IMagicLinkConfiguration> modifyAction)
        {
            WithFeature<MagicLinkFeature>(x => x.WithConfiguration(modifyAction));
        }

        private OwnIdConfigurationBuilder WithFeature<TFeature>(Func<TFeature, TFeature> setupFunc)
            where TFeature : class, IFeature, new()
        {
            AddOrUpdateFeature(setupFunc(Configuration.FindFeature<TFeature>() ?? new TFeature()));
            return this;
        }
    }
}