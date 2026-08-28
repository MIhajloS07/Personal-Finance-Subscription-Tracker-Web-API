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
        public async Task<ActionResult<IEnumerable<Subscription>>> GetAllSubscriptions()
        {
            return await _context.Subscriptions.ToListAsync();
        }

        /// <summary>
        /// get all subscription by id 
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
        public async Task<ActionResult<SubscriptionDto>> GetSubscriptionsByUserId(int userId)
        {
            string cacheKey = $"user_subscription_{userId}";
            //check in redis
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedSubscriptionDto = JsonSerializer.Deserialize<SubscriptionDto>(cachedData);
                return Ok(cachedSubscriptionDto);
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return NotFound($"User with id - {userId} not found.");

            var subscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            var subscriptionDtos = subscriptions.Select(MapToSubscriptionDto).ToList();

            // Write data in redis 5 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(subscriptionDtos), cacheOptions);

            return Ok(subscriptionDtos);
        }

        /// <summary>
        /// get all subscription by name
        /// </summary>
        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<IEnumerable<Subscription>>> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name parameter cannot be empty.");

            string cacheKey = $"sub_search_{name.Trim().ToLower()}";

            //check in redis
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedSubscription = JsonSerializer.Deserialize<IEnumerable<Subscription>>(cachedData);
                return Ok(cachedSubscription);
            }

            // if data is not in Redis, check database
            var subscriptions = await _context.Subscriptions
                .Where(x => x.Name.ToLower().Contains(name.ToLower()))
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
        public async Task<ActionResult<Subscription>> Create(Subscription subscription)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == subscription.UserId);
            if (!userExists)
            {
                return BadRequest($"User with ID {subscription.UserId} does not exists.");
            }
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // returns status 201, url to new resource and object
            return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
        }
        #endregion

        #region put_methods
        /// <summary>
        /// Update an existing subscription and invalidate Redis cache
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Subscription updatedSubscription)
        {
            if (id != updatedSubscription.Id)
                return BadRequest("Mismatched Subscription ID.");

            var existingSubscription = await _context.Subscriptions.FindAsync(id);
            if (existingSubscription == null)
                return NotFound($"Subscription with id - {id} not found.");

            // Check if new User with new id exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == updatedSubscription.UserId);
            if (!userExists)
                return BadRequest($"User with ID {updatedSubscription.UserId} does not exists.");

            // Update values
            existingSubscription.Name = updatedSubscription.Name;
            existingSubscription.Price = updatedSubscription.Price;
            existingSubscription.Currency = updatedSubscription.Currency;
            existingSubscription.PaymentDate = updatedSubscription.PaymentDate;
            existingSubscription.Category = updatedSubscription.Category;
            existingSubscription.UserId = updatedSubscription.UserId;

            await _context.SaveChangesAsync();

            // delete cache from redis
            string cacheKey = $"subscription_{id}";
            await _cache.RemoveAsync(cacheKey);

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

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            // Delete cache from redis
            string cacheKey = $"subscription_{id}";
            await _cache.RemoveAsync(cacheKey);

            return NoContent();
        }
        #endregion
        #region helper methods
        private object MapToSubscriptionDto(Subscription subscription)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}