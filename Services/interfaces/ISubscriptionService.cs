using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;

namespace Personal_Finance___Subscription_Tracker_API.Services.interfaces
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<SubscriptionDto>> GetAllAsync();

        Task<SubscriptionDto?> GetByIdAsync(int id);

        Task<IEnumerable<SubscriptionDto>?> GetByUserIdAsync(int userId);

        Task<IEnumerable<SubscriptionDto>?> GetByNameAsync(string name);

        Task<SubscriptionDto?> CreateAsync(
            CreateSubscriptionDto createSubscriptionDto);

        Task<bool> UpdateAsync(
            int id,
            UpdateSubscriptionDto updateSubscriptionDto);

        Task<bool> DeleteAsync(int id);
    }
}
