using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Personal_Finance___Subscription_Tracker_API.Data;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription
using Personal_Finance___Subscription_Tracker_API.DTOs.User;
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
        /// Get all users with subscriptions mapped to DTO-s
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Subscriptions)
                .AsNoTracking()
                .ToListAsync();
            var userDtos = users.Select(MapToUserDto).ToList();
            return Ok(userDtos);
        }


        /// <summary>
        /// Get users with subscription by user ID 
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
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
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound($"User with id - {id} not found.");

            var userDto = MapToUserDto(user);

            // write data in redis 5 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(userDto, _jsonOptions), cacheOptions);

            return Ok(userDto);
        }


        /// <summary>
        /// Get users with subscription by email of user
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
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
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound($"User with email - {email} not found.");

            var userDto = MapToUserDto(user);

            // write data in redis 5 min
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(userDto, _jsonOptions), cacheOptions);

            return Ok(userDto);
        }
        #endregion
        #region post_methods
        /// <summary>
        /// Create a new user (with unique Email validation)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(CreateUserDto createUserDto)
        {
            if (string.IsNullOrWhiteSpace(createUserDto.Email))
                return BadRequest("Email field cannot be empty. ");

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == createUserDto.Email.Trim().ToLower());
            if (emailExists)
                return BadRequest($"User with email - {createUserDto.Email} already exists.");

            var user = new User
            {
                Email = createUserDto.Email.Trim(),
                PasswordHash = createUserDto.Password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = MapToUserDto(user);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userDto);
        }
        #endregion
        #region put_methods
        /// <summary>
        /// Update user details and invalidate Redis cache
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateUserDto updateUserDto)
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
                return NotFound($"User with id - {id} not found.");

            string newCleanEmail = updateUserDto.Email.Trim().ToLower();

            var emailTakenByOther = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == newCleanEmail && u.Id != id);

            if (emailTakenByOther)
                return BadRequest($"Email '{updateUserDto.Email}' is already in use by another user.");

            string oldEmail = existingUser.Email.Trim().ToLower();

            existingUser.Email = updateUserDto.Email.Trim();

            if (!string.IsNullOrWhiteSpace(updateUserDto.NewPassword))
                existingUser.PasswordHash = updateUserDto.NewPassword;

            await _context.SaveChangesAsync();

            // Invalidate cache in Redis
            await _cache.RemoveAsync($"user_{id}");
            await _cache.RemoveAsync($"user_email_{oldEmail}");
            await _cache.RemoveAsync($"user_email_{newCleanEmail}");

            return NoContent();
        }
        #endregion
        #region delete_methods
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound($"User with id - {id} not found.");

            string userEmail = user.Email.Trim().ToLower();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Invalidate cache in Redis
            await _cache.RemoveAsync($"user_{id}");
            await _cache.RemoveAsync($"user_email_{userEmail}");

            return NoContent();
        }
        #endregion
        #region helper methods (Mapping)
        /// <summary>
        /// Private helper method to map User entity to UserDto
        /// </summary>
        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Subscriptions = user.Subscriptions?.Select(s => new SubscriptionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Price = s.Price,
                    Currency = s.Currency,
                    PaymentDate = s.PaymentDate,
                    Category = s.Category,
                    UserId = s.UserId,
                }).ToList() ?? new List<SubscriptionDto>()
            };
        }
        #endregion
    }
}
