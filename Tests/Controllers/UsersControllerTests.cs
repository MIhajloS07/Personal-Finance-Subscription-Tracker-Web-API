using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Personal_Finance___Subscription_Tracker_API.Controllers;
using Personal_Finance___Subscription_Tracker_API.DTOs.User;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;
using Personal_Finance___Subscription_Tracker_API.Tests.TestData;
using Xunit;

namespace Personal_Finance___Subscription_Tracker_API.Tests.Controllers
{
    /// <summary>
    /// Unit tests for UsersController
    /// Tests: REST endpoints, HTTP responses, and error handling
    /// </summary>
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _controller = new UsersController(_mockUserService.Object);
        }

        #region GET Tests

        [Fact]
        public async Task GetUsers_ShouldReturnOkWithAllUsers()
        {
            // Arrange
            var users = TestDataGenerator.GenerateUsers(3)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Subscriptions = new List<SubscriptionDto>()
                })
                .ToList();

            _mockUserService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _controller.GetUsers();

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            
            var returnedUsers = okResult.Value as IEnumerable<UserDto>;
            returnedUsers.Should().HaveCount(3);
            _mockUserService.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUserById_WithValidId_ShouldReturnOkWithUser()
        {
            // Arrange
            var userId = 1;
            var userDto = new UserDto
            {
                Id = userId,
                Email = "user@example.com",
                Subscriptions = new List<SubscriptionDto>()
            };

            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            
            var returnedUser = okResult.Value as UserDto;
            returnedUser.Email.Should().Be("user@example.com");
            _mockUserService.Verify(s => s.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 9999;
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync((UserDto)null);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetUserByEmail_WithValidEmail_ShouldReturnOkWithUser()
        {
            // Arrange
            var email = "user@example.com";
            var userDto = new UserDto
            {
                Id = 1,
                Email = email,
                Subscriptions = new List<SubscriptionDto>()
            };

            _mockUserService.Setup(s => s.GetByEmailAsync(email)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUserByEmail(email);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            
            var returnedUser = okResult.Value as UserDto;
            returnedUser.Email.Should().Be(email);
        }

        [Fact]
        public async Task GetUserByEmail_WithEmptyEmail_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.GetUserByEmail("");

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetUserByEmail_WithNonExistentEmail_ShouldReturnNotFound()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _mockUserService.Setup(s => s.GetByEmailAsync(email)).ReturnsAsync((UserDto)null);

            // Act
            var result = await _controller.GetUserByEmail(email);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task Create_WithValidUser_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var createDto = new CreateUserDto
            {
                Email = "newuser@example.com",
                Password = "hash123"
            };

            var createdUserDto = new UserDto
            {
                Id = 1,
                Email = createDto.Email,
                Subscriptions = new List<SubscriptionDto>()
            };

            _mockUserService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdUserDto);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult.Should().NotBeNull();
            createdResult.StatusCode.Should().Be(201);
            createdResult.ActionName.Should().Be(nameof(UsersController.GetUserById));
            
            var returnedUser = createdResult.Value as UserDto;
            returnedUser.Email.Should().Be(createDto.Email);
        }

        [Fact]
        public async Task Create_WithDuplicateEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateUserDto
            {
                Email = "duplicate@example.com",
                Password = "hash123"
            };

            _mockUserService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync((UserDto)null);

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
        public async Task Update_WithValidUser_ShouldReturnNoContent()
        {
            // Arrange
            var userId = 1;
            var updateDto = new UpdateUserDto
            {
                Email = "updated@example.com"
            };

            _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(userId, updateDto);

            // Assert
            var noContentResult = result as NoContentResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be(204);
            _mockUserService.Verify(s => s.UpdateAsync(userId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Update_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 9999;
            var updateDto = new UpdateUserDto { Email = "any@example.com" };

            _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto)).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(userId, updateDto);

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
            var userId = 1;
            _mockUserService.Setup(s => s.DeleteAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            var noContentResult = result as NoContentResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be(204);
            _mockUserService.Verify(s => s.DeleteAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Delete_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 9999;
            _mockUserService.Setup(s => s.DeleteAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        #endregion
    }
}