using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Personal_Finance___Subscription_Tracker_API.Data;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.Model;
using System.Text.Json;

namespace Personal_Finance___Subscription_Tracker_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;

        public SubscriptionsController(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        #region get_methods
        /// <summary>
        /// get all subscription
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetAllSubscriptions()
        {
            string cacheKey = "all_subscriptions";
            //check in redis
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedSubscriptions = JsonSerializer.Deserialize<List<SubscriptionDto>>(cachedData);
                return Ok(cachedSubscriptions);
            }

            // all subscriptions
            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .ToListAsync();

            var subscriptionsDtos = subscriptions.Select(MapToSubscriptionDto).ToList();

            // Write in Redis cache 5mins
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(subscriptionsDtos), cacheOptions);

            return Ok(subscriptionsDtos);
        }

        /// <summary>
        /// get subscription by id 
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SubscriptionDto>> GetById(int id)
        {
            string cacheKey = $"subscription_{id}";
            //check in redis
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedSubscriptionDto = JsonSerializer.Deserialize<SubscriptionDto>(cachedData);
                return Ok(cachedSubscriptionDto);
            }

            // if data is not in Redis, check database
            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (subscription == null)
                return NotFound($"Subscription with id - {id} not found.");

            var subscriptionDto = MapToSubscriptionDto(subscription);

            // Write data in redis 5 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(subscriptionDto), cacheOptions);

            return Ok(subscriptionDto);
        }

        /// <summary>
        /// Get all subscriptions for a specific user by UserId
        /// </summary>
        /// 
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetSubscriptionsByUserId(int userId)
        {
            string cacheKey = $"user_subscription_{userId}";
            //check in redis
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedSubscriptionDto = JsonSerializer.Deserialize<List<SubscriptionDto>>(cachedData);
                return Ok(cachedSubscriptionDto);
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return NotFound($"User with id - {userId} not found.");

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

            // Write data in redis 5 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(subscriptions), cacheOptions);

            return Ok(subscriptions);
        }

        /// <summary>
        /// get all subscription by name
        /// </summary>
        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name parameter cannot be empty.");

            string normalizedName = name.Trim().ToLowerInvariant();
            var cacheKey = $"sub_search_{normalizedName}";

            //check in redis
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedSubscription = JsonSerializer.Deserialize<List<SubscriptionDto>>(cachedData);
                return Ok(cachedSubscription);
            }

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
                return NotFound($"No subscriptions found maching name - {name}.");

            // Write data in redis 1 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(subscriptions), cacheOptions);

            return Ok(subscriptions);
        }
        #endregion

        #region post_methods
        /// <summary>
        /// Create a new subscription
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SubscriptionDto>> Create(CreateSubscriptionDto createSubscriptionDto)
        {
            var user = await _context.Users.FindAsync(createSubscriptionDto.UserId);
            if (user == null)
                return BadRequest($"User with ID {createSubscriptionDto.UserId} does not exists.");

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

            // returns status 201, url to new resource and object
            return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscriptionDto);
        }
        #endregion

        #region put_methods
        /// <summary>
        /// Update an existing subscription and invalidate Redis cache
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateSubscriptionDto updatedSubscriptionDto)
        {
            var existingSubscription = await _context.Subscriptions.FindAsync(id);
            if (existingSubscription == null)
                return NotFound($"Subscription with id - {id} not found.");

            int oldUserId = existingSubscription.UserId;
            var newUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == updatedSubscriptionDto.UserId);

            if (newUser == null)
                return BadRequest($"User with ID {updatedSubscriptionDto.UserId} does not exist.");

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

            return NoContent(); // HTTP 204
        }
        #endregion
        #region delete_methods
        /// <summary>
        /// Delete subscription and remove from Redis cache
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound($"Subscription with id - {id} not found.");

            var user = await _context.Users.FindAsync(subscription.UserId);

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            // Delete cache from redis
            await _cache.RemoveAsync($"subscription_{id}");
            await InvalidateRelatedCacheAsync(subscription.UserId, user?.Email);

            return NoContent();
        }
        #endregion
        #region helper_methods
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
        #endregion
    }
}