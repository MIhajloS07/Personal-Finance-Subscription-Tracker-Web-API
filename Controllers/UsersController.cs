using Microsoft.AspNetCore.Mvc;
using Personal_Finance___Subscription_Tracker_API.DTOs.User;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;

namespace Personal_Finance___Subscription_Tracker_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        #region get_methods

        /// <summary>
        /// Get all users with subscriptions 
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        /// <summary>
        /// Get users with subscription by user ID 
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with id - {id} not found.");
            return Ok(user);
        }


        /// <summary>
        /// Get users with subscription by email of user
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email parameter cannot be empty.");

            var user = await _userService.GetByEmailAsync(email);

            if (user == null)
                return NotFound($"User with email - {email} not found.");

            return Ok(user);
        }
        #endregion
        #region post_methods
        /// <summary>
        /// Create a new user (with unique Email validation)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(CreateUserDto createUserDto)
        {
            var user = await _userService.CreateAsync(createUserDto);

            if (user == null)
                return BadRequest(
                    "User could not be created. Email may be empty or already in use.");

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        #endregion
        #region put_methods
        /// <summary>
        /// Update user details and invalidate Redis cache
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateUserDto updateUserDto)
        {
            var updated = await _userService.UpdateAsync(
                id,
                updateUserDto);

            if (!updated)
                return NotFound(
                    $"User with id - {id} not found or email is already in use.");

            return NoContent();
        }
        #endregion
        #region delete_methods
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _userService.DeleteAsync(id);

            if (!deleted)
                return NotFound($"User with id - {id} not found.");

            return NoContent();
        }
        #endregion
    }
}
