using Microsoft.AspNetCore.Mvc;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;

namespace Personal_Finance___Subscription_Tracker_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        #region get_methods
        /// <summary>
        /// Get all subscriptions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetAllSubscriptions()
        {
            var subscriptions = await _subscriptionService.GetAllAsync();
            return Ok(subscriptions);
        }

        /// <summary>
        /// Get subscription by id
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SubscriptionDto>> GetById(int id)
        {
            var subscription = await _subscriptionService.GetByIdAsync(id);
            if (subscription == null)
                return NotFound($"Subscription with id - {id} not found.");

            return Ok(subscription);
        }

        /// <summary>
        /// Get all subscriptions for a specific user by UserId
        /// </summary>
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetSubscriptionsByUserId(int userId)
        {
            var subscriptions = await _subscriptionService.GetByUserIdAsync(userId);
            if (subscriptions == null)
                return NotFound($"User with id - {userId} not found.");

            return Ok(subscriptions);
        }

        /// <summary>
        /// Get all subscriptions by name
        /// </summary>
        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name parameter cannot be empty.");

            var subscriptions = await _subscriptionService.GetByNameAsync(name);
            if (subscriptions == null)
                return NotFound($"No subscriptions found matching name - {name}.");

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
            var subscription = await _subscriptionService.CreateAsync(createSubscriptionDto);
            if (subscription == null)
                return BadRequest($"User with ID {createSubscriptionDto.UserId} does not exist.");

            return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
        }
        #endregion

        #region put_methods
        /// <summary>
        /// Update an existing subscription
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateSubscriptionDto updatedSubscriptionDto)
        {
            var updated = await _subscriptionService.UpdateAsync(id, updatedSubscriptionDto);
            if (!updated)
                return NotFound($"Subscription with id - {id} not found.");

            return NoContent();
        }
        #endregion

        #region delete_methods
        /// <summary>
        /// Delete subscription
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _subscriptionService.DeleteAsync(id);
            if (!deleted)
                return NotFound($"Subscription with id - {id} not found.");

            return NoContent();
        }
        #endregion
    }
}
