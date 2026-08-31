using Microsoft.EntityFrameworkCore;
using Personal_Finance___Subscription_Tracker_API.Data;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.Model;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;

namespace Personal_Finance___Subscription_Tracker_API.Services.implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cache;

        public SubscriptionService(AppDbContext context, ICacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IEnumerable<SubscriptionDto>> GetAllAsync()
        {
            string cacheKey = "all_subscriptions";
            var cachedSubscriptions = await _cache.GetAsync<List<SubscriptionDto>>(cacheKey);
            if (cachedSubscriptions != null)
                return cachedSubscriptions;
            // all subscriptions
            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Select(s => new SubscriptionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Price = s.Price,
                    Currency = s.Currency,
                    PaymentDate = s.PaymentDate,
                    Category = s.Category,
                    UserId = s.UserId
                })
                .ToListAsync();

            await _cache.SetAsync(cacheKey, subscriptions, TimeSpan.FromMinutes(5));
            return subscriptions;
        }

        public async Task<SubscriptionDto?> GetByIdAsync(int id)
        {
            string cacheKey = $"subscription_{id}";
            var cachedSubscription = await _cache.GetAsync<SubscriptionDto>(cacheKey);

            if (cachedSubscription != null)
                return cachedSubscription;

            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return null;

            var subscriptionDto = MapToSubscriptionDto(subscription);

            await _cache.SetAsync(cacheKey, subscriptionDto, TimeSpan.FromMinutes(5));

            return subscriptionDto;
        }

        public async Task<IEnumerable<SubscriptionDto>?> GetByUserIdAsync(int userId)
        {
            string cacheKey = $"user_subscription_{userId}";

            var cachedSubscriptions = await _cache.GetAsync<List<SubscriptionDto>>(cacheKey);
            if (cachedSubscriptions != null)
                return cachedSubscriptions;

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId);

            if (!userExists)
                return null;

            var subscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .AsNoTracking()
                .Select(s => new SubscriptionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Price = s.Price,
                    Currency = s.Currency,
                    PaymentDate = s.PaymentDate,
                    Category = s.Category,
                    UserId = s.UserId
                })
                .ToListAsync();


            await _cache.SetAsync(cacheKey, subscriptions, TimeSpan.FromMinutes(5));

            return subscriptions;
        }

        public async Task<IEnumerable<SubscriptionDto>?> GetByNameAsync(string name)
        {
            string normalizedName = name.Trim().ToLowerInvariant();
            var cacheKey = $"sub_search_{normalizedName}";

            var cachedSubscriptions = await _cache.GetAsync<List<SubscriptionDto>>(cacheKey);
            if (cachedSubscriptions != null)
                return cachedSubscriptions;

            // if data is not in Redis, check database
            var subscriptions = await _context.Subscriptions
                .Where(x => EF.Functions.Like(x.Name.ToLower(), $"%{normalizedName}%"))
                .AsNoTracking()
                .Select(s => new SubscriptionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Price = s.Price,
                    Currency = s.Currency,
                    PaymentDate = s.PaymentDate,
                    Category = s.Category,
                    UserId = s.UserId
                })
                .ToListAsync();

            if (!subscriptions.Any())
                return null;

            await _cache.SetAsync(cacheKey, subscriptions, TimeSpan.FromSeconds(60));

            return subscriptions;
        }

        public async Task<SubscriptionDto?> CreateAsync(CreateSubscriptionDto createSubscriptionDto)
        {
            var user = await _context.Users.FindAsync(createSubscriptionDto.UserId);
            if (user == null)
                return null;

            var subscription = new Subscription
            {
                Name = createSubscriptionDto.Name.Trim(),
                Price = createSubscriptionDto.Price,
                Currency = createSubscriptionDto.Currency.Trim(),
                PaymentDate = createSubscriptionDto.PaymentDate,
                Category = createSubscriptionDto.Category,
                UserId = createSubscriptionDto.UserId
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var subscriptionDto = MapToSubscriptionDto(subscription);

            await InvalidateRelatedCacheAsync(subscription.UserId, user.Email);

            return subscriptionDto;
        }

        public async Task<bool> UpdateAsync(int id, UpdateSubscriptionDto updatedSubscriptionDto)
        {
            var existingSubscription = await _context.Subscriptions.FindAsync(id);
            if (existingSubscription == null)
                return false;

            int oldUserId = existingSubscription.UserId;

            var newUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == updatedSubscriptionDto.UserId);

            if (newUser == null)
                return false;

            string? oldUserEmail = oldUserId == updatedSubscriptionDto.UserId
                ? newUser.Email
                : await _context.Users.Where(u => u.Id == oldUserId).Select(u => u.Email).FirstOrDefaultAsync();

            // Update values
            existingSubscription.Name = updatedSubscriptionDto.Name.Trim();
            existingSubscription.Price = updatedSubscriptionDto.Price;
            existingSubscription.Currency = updatedSubscriptionDto.Currency.Trim();
            existingSubscription.PaymentDate = updatedSubscriptionDto.PaymentDate;
            existingSubscription.Category = updatedSubscriptionDto.Category;
            existingSubscription.UserId = updatedSubscriptionDto.UserId;

            await _context.SaveChangesAsync();

            // delete cache from redis
            await _cache.RemoveAsync($"subscription_{id}");

            await InvalidateRelatedCacheAsync(oldUserId, oldUserEmail);

            if (oldUserId != updatedSubscriptionDto.UserId)
            {
                await InvalidateRelatedCacheAsync(updatedSubscriptionDto.UserId, newUser.Email);
            }

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return false;

            var user = await _context.Users.FindAsync(subscription.UserId);

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            // Delete cache from redis
            await _cache.RemoveAsync($"subscription_{id}");

            await InvalidateRelatedCacheAsync(subscription.UserId, user?.Email);

            return true;
        }

        private static SubscriptionDto MapToSubscriptionDto(Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                Name = subscription.Name,
                Price = subscription.Price,
                Currency = subscription.Currency,
                PaymentDate = subscription.PaymentDate,
                Category = subscription.Category,
                UserId = subscription.UserId
            };
        }

        private async Task InvalidateRelatedCacheAsync(int userId, string? userEmail)
        {
            await _cache.RemoveAsync("all_subscriptions");
            await _cache.RemoveAsync($"user_subscription_{userId}");
            await _cache.RemoveAsync($"user_{userId}");

            if (!string.IsNullOrWhiteSpace(userEmail))
                await _cache.RemoveAsync($"user_email_{userEmail.Trim().ToLowerInvariant()}");
        }
    }
}