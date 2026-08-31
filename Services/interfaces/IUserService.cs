using Personal_Finance___Subscription_Tracker_API.DTOs.User;

namespace Personal_Finance___Subscription_Tracker_API.Services.interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();

        Task<UserDto?> GetByIdAsync(int id);

        Task<UserDto?> GetByEmailAsync(string email);

        Task<UserDto?> CreateAsync(CreateUserDto createUserDto);

        Task<bool> UpdateAsync(
            int id,
            UpdateUserDto updateUserDto);

        Task<bool> DeleteAsync(int id);
    }
}
