using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Extensibility.Cache;
using OwnID.Store;
using OwnID.Web.Extensibility;
using OwnID.Web.Store;

namespace OwnID.Web.Features
{
    public class CacheStoreFeature : IFeature
    {
        private ServiceLifetime _serviceLifetime;
        private Action<IServiceCollection> _servicesInitialization;
        private Type _storeType;
        private Func<IServiceProvider, ICacheStore> _cacheStoreFactory;

        public void ApplyServices(IServiceCollection services)
        {
            _servicesInitialization?.Invoke(services);
            
            if (_cacheStoreFactory != null)
                services.TryAdd(new ServiceDescriptor(typeof(ICacheStore), _cacheStoreFactory, _serviceLifetime));
            else if (_storeType != null)
                services.TryAdd(new ServiceDescriptor(typeof(ICacheStore), _storeType, _serviceLifetime));
        }

        public IFeature FillEmptyWithOptional()
        {
            if (_storeType == null)
            {
                _storeType = typeof(InMemoryCacheStore);
                _serviceLifetime = ServiceLifetime.Singleton;
            }

            return this;
        }

        public void Validate()
        {
            if (_storeType == null)
                throw new InvalidOperationException("Store Type can not be null");
        }

        public CacheStoreFeature UseInMemoryStore()
        {
            _storeType = typeof(InMemoryCacheStore);
            _serviceLifetime = ServiceLifetime.Singleton;
            return this;
        }

        public CacheStoreFeature UseWebCacheStore()
        {
            _servicesInitialization = services => services.AddMemoryCache();

            _storeType = typeof(WebCacheStore);
            _serviceLifetime = ServiceLifetime.Singleton;

            return this;
        }

        public CacheStoreFeature UseStore<TStore>(ServiceLifetime serviceLifetime) where TStore : class, ICacheStore
        {
            _serviceLifetime = serviceLifetime;
            _storeType = typeof(TStore);

            return this;
        }

        public CacheStoreFeature UseStore<TStore>(ServiceLifetime serviceLifetime,
            Func<IServiceProvider, TStore> cacheStoreFactory) where TStore : class, ICacheStore
        {
            _serviceLifetime = serviceLifetime;
            _storeType = typeof(TStore);
            _cacheStoreFactory = cacheStoreFactory;

            return this;
        }
    }
}