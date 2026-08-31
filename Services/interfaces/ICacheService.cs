namespace Personal_Finance___Subscription_Tracker_API.Services.interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(
            string key, 
            T value, 
            TimeSpan expiration);
        Task RemoveAsync(string key);
    }
}
