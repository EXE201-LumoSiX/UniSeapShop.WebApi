using UniSeapShop.Application.Interfaces;

namespace UniSeapShop.Application.Services
{
    public class RedisCacheService : ICacheService
    {
        public Task ClearAllAppCachesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task<T?> GetAsync<T>(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            throw new NotImplementedException();
        }
    }
}
