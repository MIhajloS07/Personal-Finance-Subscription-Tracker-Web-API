using Personal_Finance___Subscription_Tracker_API.Services.interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Personal_Finance___Subscription_Tracker_API.Services.implementations
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cacheService;
        public RedisCacheService(IDistributedCache cacheService) 
        {
            _cacheService = cacheService;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedData = await _cacheService.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedData))
                return default;
            return JsonSerializer.Deserialize<T>(cachedData);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cacheService.SetStringAsync(
                key,
                JsonSerializer.Serialize(value),
                options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cacheService.RemoveAsync(key);
        }   
    }  
}

