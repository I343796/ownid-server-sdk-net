using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Json;
using OwnID.Extensibility.Logs;
using StackExchange.Redis;

namespace OwnID.Redis
{
    public class RedisCacheStore : ICacheStore
    {
        private readonly string _keyPrefix;
        private readonly ILogger<RedisCacheStore> _logger;
        private readonly IDatabase _redisDb;

        public RedisCacheStore(string configurationString, IOwnIdCoreConfiguration coreConfiguration,
            ILogger<RedisCacheStore> logger)
        {
            _logger = logger;
            _keyPrefix = coreConfiguration.DID;

            if (string.IsNullOrEmpty(configurationString))
                throw new ArgumentException("No configuration was provided");

            _redisDb = ConnectionMultiplexer.Connect(configurationString).GetDatabase();
        }

        public void Set(string key, CacheItem data, TimeSpan expiration)
        {
            var serializedData = OwnIdSerializer.Serialize(data);
            var compKey = GetCustomerKey(key);

            var isSuccess = _redisDb.StringSet(compKey, serializedData, expiration);
            _logger.Log(LogLevel.Debug, () => $"set(key -> '{compKey}' value -> '{serializedData}')");

            if (!isSuccess)
                throw new Exception($"Can not set element to redis with context {data.Context}");
        }

        public async Task SetAsync(string key, CacheItem data, TimeSpan expiration)
        {
            var serializedData = OwnIdSerializer.Serialize(data);
            var compKey = GetCustomerKey(key);

            var isSuccess = await _redisDb.StringSetAsync(compKey, serializedData, expiration);
            _logger.Log(LogLevel.Debug, () => $"set(key -> '{compKey}' value -> '{serializedData}')");

            if (!isSuccess)
                throw new Exception($"Can not set element to redis with context {data.Context}");
        }

        public CacheItem Get(string key)
        {
            var compKey = GetCustomerKey(key);
            var item = _redisDb.StringGet($"{_keyPrefix}{key}");

            if (item.IsNullOrEmpty)
                return null;

            _logger.Log(LogLevel.Debug, () => $"get(key -> '{compKey}') -> Response: '{item.ToString()}'");
            return OwnIdSerializer.Deserialize<CacheItem>(item.ToString());
        }

        public async Task<CacheItem> GetAsync(string key)
        {
            var compKey = GetCustomerKey(key);
            var item = await _redisDb.StringGetAsync(compKey);

            if (item.IsNullOrEmpty)
                return null;

            _logger.Log(LogLevel.Debug, () => $"get(key -> '{compKey}') -> Response: '{item.ToString()}'");
            return OwnIdSerializer.Deserialize<CacheItem>(item.ToString());
        }

        public void Remove(string key)
        {
            var compKey = GetCustomerKey(key);
            _logger.Log(LogLevel.Debug, () => $"delete(key -> '{compKey}')");
            _redisDb.KeyDelete(compKey, CommandFlags.FireAndForget);
        }

        public async Task RemoveAsync(string key)
        {
            var compKey = GetCustomerKey(key);
            _logger.Log(LogLevel.Debug, () => $"delete(key -> '{compKey}')");
            await _redisDb.KeyDeleteAsync(compKey, CommandFlags.FireAndForget);
        }

        public async Task<(long keysCount, long itemsSize)> GetMemoryStatsAsync()
        {
            var result = (await _redisDb.ExecuteAsync("memory", "stats")).ToDictionary();
            return (long.Parse(result["keys.count"].ToString() ?? "0"),
                long.Parse(result["dataset.bytes"].ToString() ?? "0"));
        }

        private string GetCustomerKey(string key)
        {
            return $"{_keyPrefix}{key}";
        }
    }
}