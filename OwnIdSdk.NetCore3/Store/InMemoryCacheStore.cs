using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OwnIdSdk.NetCore3.Store
{
    public class InMemoryCacheStore : ICacheStore
    {
        private readonly ConcurrentDictionary<string, CacheItem> _store;

        public InMemoryCacheStore()
        {
            _store = new ConcurrentDictionary<string, CacheItem>();
        }

        public void Set(string key, CacheItem data)
        {
            _store.AddOrUpdate(key, data, (s, o) => data);
        }

        public async Task SetAsync(string key, CacheItem data)
        {
            Set(key, data);
            await Task.CompletedTask;
        }

        public CacheItem Get(string key)
        {
            return !_store.TryGetValue(key, out var item) ? null : item;
        }

        public async Task<CacheItem> GetAsync(string key)
        {
            return await Task.FromResult(Get(key));
        }
    }
}