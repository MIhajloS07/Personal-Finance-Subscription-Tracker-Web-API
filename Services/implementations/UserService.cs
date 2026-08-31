using Microsoft.EntityFrameworkCore;
using Personal_Finance___Subscription_Tracker_API.Data;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.DTOs.User;
using Personal_Finance___Subscription_Tracker_API.Model;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;

namespace Personal_Finance___Subscription_Tracker_API.Services.implementations
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cache;

        public UserService(AppDbContext context, ICacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _context.Users
                .Include(u => u.Subscriptions)
                .AsNoTracking()
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            string cacheKey = $"user_{id}";

            var cachedUser =
                await _cache.GetAsync<UserDto>(cacheKey);

            if (cachedUser != null)
                return cachedUser;

            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return null;

            var userDto = MapToUserDto(user);

            await _cache.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(5));

            return userDto;
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            string cacheKey = $"user_email_{normalizedEmail}";

            var cachedUser =
                await _cache.GetAsync<UserDto>(cacheKey);

            if (cachedUser != null)
                return cachedUser;

            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
                return null;

            var userDto = MapToUserDto(user);

            await _cache.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(5));

            return userDto;
        }

        public async Task<UserDto?> CreateAsync(CreateUserDto createUserDto)
        {
            if (string.IsNullOrWhiteSpace(createUserDto.Email))
                return null;

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == createUserDto.Email.Trim().ToLowerInvariant());

            if (emailExists)
                return null;

            var user = new User
            {
                Email = createUserDto.Email.Trim(),
                PasswordHash = createUserDto.Password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task<bool> UpdateAsync(int id, UpdateUserDto updateUserDto)
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
                return false;

            string newCleanEmail = updateUserDto.Email.Trim().ToLowerInvariant();

            var emailTakenByOther = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == newCleanEmail && u.Id != id);

            if (emailTakenByOther)
                return false;

            string oldEmail = existingUser.Email.Trim().ToLowerInvariant();

            existingUser.Email = updateUserDto.Email.Trim();

            if (!string.IsNullOrWhiteSpace(updateUserDto.NewPassword))
                existingUser.PasswordHash = updateUserDto.NewPassword;

            await _context.SaveChangesAsync();

            // Invalidate cache in Redis
            await _cache.RemoveAsync($"user_{id}");
            await _cache.RemoveAsync($"user_email_{oldEmail}");
            await _cache.RemoveAsync($"user_email_{newCleanEmail}");

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            string userEmail = user.Email.Trim().ToLower();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Invalidate cache in Redis
            await _cache.RemoveAsync($"user_{id}");
            await _cache.RemoveAsync($"user_email_{userEmail}");

            return true;
        }

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
    }
}
