using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Personal_Finance___Subscription_Tracker_API.Controllers;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;
using Personal_Finance___Subscription_Tracker_API.Tests.TestData;
using Xunit;

namespace Personal_Finance___Subscription_Tracker_API.Tests.Controllers
{
    /// <summary>
    /// Unit tests for SubscriptionsController
    /// Tests: REST endpoints, filtering, and error handling
    /// </summary>
    public class SubscriptionsControllerTests
    {
        private readonly Mock<ISubscriptionService> _mockSubscriptionService;
        private readonly SubscriptionsController _controller;

        public SubscriptionsControllerTests()
        {
            _mockSubscriptionService = new Mock<ISubscriptionService>();
            _controller = new SubscriptionsController(_mockSubscriptionService.Object);
        }

        #region GET Tests

        [Fact]
        public async Task GetAllSubscriptions_ShouldReturnOkWithAllSubscriptions()
        {
            // Arrange
            var subscriptions = TestDataGenerator.GenerateSubscriptions(3)
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
                .ToList();

            _mockSubscriptionService.Setup(s => s.GetAllAsync()).ReturnsAsync(subscriptions);

            // Act
            var result = await _controller.GetAllSubscriptions();

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            
            var returnedSubscriptions = okResult.Value as IEnumerable<SubscriptionDto>;
            returnedSubscriptions.Should().HaveCount(3);
            _mockSubscriptionService.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_WithValidId_ShouldReturnOkWithSubscription()
        {
            // Arrange
            var subscriptionId = 1;
            var subscriptionDto = new SubscriptionDto
            {
                Id = subscriptionId,
                Name = "Netflix",
                Price = 15.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Entertainment",
                UserId = 1
            };

            _mockSubscriptionService.Setup(s => s.GetByIdAsync(subscriptionId)).ReturnsAsync(subscriptionDto);

            // Act
            var result = await _controller.GetById(subscriptionId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            
            var returned = okResult.Value as SubscriptionDto;
            returned.Name.Should().Be("Netflix");
        }

        [Fact]
        public async Task GetById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var subscriptionId = 9999;
            _mockSubscriptionService.Setup(s => s.GetByIdAsync(subscriptionId)).ReturnsAsync((SubscriptionDto)null);

            // Act
            var result = await _controller.GetById(subscriptionId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetSubscriptionsByUserId_WithValidUserId_ShouldReturnUserSubscriptions()
        {
            // Arrange
            var userId = 1;
            var subscriptions = TestDataGenerator.GenerateSubscriptions(2, userId)
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
                .ToList();

            _mockSubscriptionService.Setup(s => s.GetByUserIdAsync(userId)).ReturnsAsync(subscriptions);

            // Act
            var result = await _controller.GetSubscriptionsByUserId(userId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            
            var returned = okResult.Value as IEnumerable<SubscriptionDto>;
            returned.Should().HaveCount(2);
            returned.All(s => s.UserId == userId).Should().BeTrue();
        }

        [Fact]
        public async Task GetSubscriptionsByUserId_WithInvalidUserId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 9999;
            _mockSubscriptionService.Setup(s => s.GetByUserIdAsync(userId)).ReturnsAsync((List<SubscriptionDto>)null);

            // Act
            var result = await _controller.GetSubscriptionsByUserId(userId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetByName_WithValidName_ShouldReturnMatchingSubscriptions()
        {
            // Arrange
            var name = "Netflix";
            var subscriptions = new List<SubscriptionDto>
            {
                new SubscriptionDto
                {
                    Id = 1,
                    Name = "Netflix",
                    Price = 15.99m,
                    Currency = "USD",
                    PaymentDate = DateTime.UtcNow,
                    Category = "Entertainment",
                    UserId = 1
                }
            };

            _mockSubscriptionService.Setup(s => s.GetByNameAsync(name)).ReturnsAsync(subscriptions);

            // Act
            var result = await _controller.GetByName(name);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            
            var returned = okResult.Value as IEnumerable<SubscriptionDto>;
            returned.Should().HaveCount(1);
            returned.First().Name.Should().Contain("Netflix");
        }

        [Fact]
        public async Task GetByName_WithEmptyName_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.GetByName("");

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetByName_WithNonExistentName_ShouldReturnNotFound()
        {
            // Arrange
            var name = "NonExistent";
            _mockSubscriptionService.Setup(s => s.GetByNameAsync(name)).ReturnsAsync((List<SubscriptionDto>)null);

            // Act
            var result = await _controller.GetByName(name);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task Create_WithValidSubscription_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var createDto = new CreateSubscriptionDto
            {
                Name = "Netflix",
                Price = 15.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Entertainment",
                UserId = 1
            };

            var createdDto = new SubscriptionDto
            {
                Id = 1,
                Name = createDto.Name,
                Price = createDto.Price,
                Currency = createDto.Currency,
                PaymentDate = createDto.PaymentDate,
                Category = createDto.Category,
                UserId = createDto.UserId
            };

            _mockSubscriptionService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdDto);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult.Should().NotBeNull();
            createdResult.StatusCode.Should().Be(201);
            createdResult.ActionName.Should().Be(nameof(SubscriptionsController.GetById));
            
            var returned = createdResult.Value as SubscriptionDto;
            returned.Name.Should().Be("Netflix");
        }

        [Fact]
        public async Task Create_WithInvalidUserId_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateSubscriptionDto
            {
                Name = "Netflix",
                Price = 15.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Entertainment",
                UserId = 9999
            };

            _mockSubscriptionService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync((SubscriptionDto)null);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task Update_WithValidSubscription_ShouldReturnNoContent()
        {
            // Arrange
            var subscriptionId = 1;
            var updateDto = new UpdateSubscriptionDto
            {
                Name = "Netflix Premium",
                Price = 19.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Entertainment"
            };

            _mockSubscriptionService.Setup(s => s.UpdateAsync(subscriptionId, updateDto)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(subscriptionId, updateDto);

            // Assert
            var noContentResult = result as NoContentResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be(204);
            _mockSubscriptionService.Verify(s => s.UpdateAsync(subscriptionId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var subscriptionId = 9999;
            var updateDto = new UpdateSubscriptionDto
            {
                Name = "Any",
                Price = 10m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Any"
            };

            _mockSubscriptionService.Setup(s => s.UpdateAsync(subscriptionId, updateDto)).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(subscriptionId, updateDto);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task Delete_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            var subscriptionId = 1;
            _mockSubscriptionService.Setup(s => s.DeleteAsync(subscriptionId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(subscriptionId);

            // Assert
            var noContentResult = result as NoContentResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be(204);
            _mockSubscriptionService.Verify(s => s.DeleteAsync(subscriptionId), Times.Once);
        }

        [Fact]
        public async Task Delete_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var subscriptionId = 9999;
            _mockSubscriptionService.Setup(s => s.DeleteAsync(subscriptionId)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(subscriptionId);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        #endregion
    }
}
