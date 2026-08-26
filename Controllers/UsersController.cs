using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Personal_Finance___Subscription_Tracker_API.Data;
using Personal_Finance___Subscription_Tracker_API.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Personal_Finance___Subscription_Tracker_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public UsersController(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        #region get_methods

        /// <summary>
        /// Get all users with subscriptions 
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users
                .Include(u => u.Subscriptions)
                .ToListAsync();
        }


        /// <summary>
        /// Get users with subscription by user ID 
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            string cacheKey = $"user_{id}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedUser = JsonSerializer.Deserialize<User>(cachedData, _jsonOptions);
                return Ok(cachedUser);
            }
            // if data is not in redis - check in database
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound($"User with id - {id} not found.");

            // write data in redis 5 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user, _jsonOptions), cacheOptions);

            return Ok(user);
        }


        /// <summary>
        /// Get users with subscription by email of user
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            string cleanEmail = email.Trim().ToLower();
            string cacheKey = $"user_email_{cleanEmail}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedUser = JsonSerializer.Deserialize<User>(cachedData, _jsonOptions);
                return Ok(cachedUser);
            }
            // if data is not in redis - check in database
            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound($"User with email - {email} not found.");

            // write data in redis 5 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user, _jsonOptions), cacheOptions);

            return Ok(user);
        }
        #endregion
        #region post_methods
        /// <summary>
        /// Create a new user (with unique Email validation)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<User>> Create(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest("Email field cannot be empty. ");

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == user.Email.Trim().ToLower());
            if (emailExists)
                return BadRequest($"User with email - {user.Email} already exists.");

            user.Email = user.Email.Trim();

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        #endregion
        #region put_methods
        /// <summary>
        /// Update user details and invalidate Redis cache
        /// </summary>
        /// 
        #endregion
        #region delete_methods
        #endregion
    }
}
