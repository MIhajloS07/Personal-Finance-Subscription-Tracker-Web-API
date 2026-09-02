using FluentAssertions;
using Moq;
using Personal_Finance___Subscription_Tracker_API.DTOs.User;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;
using Personal_Finance___Subscription_Tracker_API.Services.implementations;
using Personal_Finance___Subscription_Tracker_API.Tests.Fixtures;
using Personal_Finance___Subscription_Tracker_API.Tests.TestData;
using Xunit;

namespace Personal_Finance___Subscription_Tracker_API.Tests.Services
{
    public class UserServiceTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly UserService _userService;

        public UserServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            var mockCache = new Mock<ICacheService>();
            _userService = new UserService(_fixture.Context, mockCache.Object);
        }

        [Fact]
        public async Task CreateAsync_WithValidUser_ShouldCreateUserSuccessfully()
        {
            await _fixture.ClearAsync();
            var createUserDto = new CreateUserDto { Email = "newuser@example.com", Password = "secure_password_123" };
            var result = await _userService.CreateAsync(createUserDto);
            result.Should().NotBeNull();
            result.Email.Should().Be(createUserDto.Email);
            result.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateEmail_ShouldReturnNull()
        {
            await _fixture.ClearAsync();
            var email = "duplicate@example.com";
            var firstUser = new CreateUserDto { Email = email, Password = "hash1" };
            var secondUser = new CreateUserDto { Email = email, Password = "hash2" };
            await _userService.CreateAsync(firstUser);
            var result = await _userService.CreateAsync(secondUser);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_WithEmptyEmail_ShouldReturnNull()
        {
            await _fixture.ClearAsync();
            var createUserDto = new CreateUserDto { Email = "", Password = "secure_hash" };
            var result = await _userService.CreateAsync(createUserDto);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_WithNullEmail_ShouldReturnNull()
        {
            await _fixture.ClearAsync();
            var createUserDto = new CreateUserDto { Email = null!, Password = "secure_hash" };
            var result = await _userService.CreateAsync(createUserDto);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            await _fixture.ClearAsync();
            var users = TestDataGenerator.GenerateUsers(3);
            foreach (var user in users)
            {
                _fixture.Context.Users.Add(user);
            }
            await _fixture.Context.SaveChangesAsync();
            var result = await _userService.GetAllAsync();
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
        {
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "getbyid@example.com");
            _fixture.Context.Users.Add(user);
            await _fixture.Context.SaveChangesAsync();
            var result = await _userService.GetByIdAsync(user.Id);
            result.Should().NotBeNull();
            result.Email.Should().Be(user.Email);
            result.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            await _fixture.ClearAsync();
            var result = await _userService.GetByIdAsync(9999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
        {
            await _fixture.ClearAsync();
            var email = "searchbyemail@example.com";
            var user = TestDataGenerator.GenerateValidUser(1, email);
            _fixture.Context.Users.Add(user);
            await _fixture.Context.SaveChangesAsync();
            var result = await _userService.GetByEmailAsync(email);
            result.Should().NotBeNull();
            result.Email.Should().Be(email);
        }

        [Fact]
        public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull()
        {
            await _fixture.ClearAsync();
            var result = await _userService.GetByEmailAsync("nonexistent@example.com");
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_WithValidUser_ShouldUpdateSuccessfully()
        {
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "update@example.com");
            _fixture.Context.Users.Add(user);
            await _fixture.Context.SaveChangesAsync();
            var updateDto = new UpdateUserDto { Email = "updated@example.com" };
            var result = await _userService.UpdateAsync(user.Id, updateDto);
            result.Should().BeTrue();
            var updatedUser = await _userService.GetByIdAsync(user.Id);
            updatedUser.Email.Should().Be("updated@example.com");
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ShouldReturnFalse()
        {
            await _fixture.ClearAsync();
            var updateDto = new UpdateUserDto { Email = "any@example.com" };
            var result = await _userService.UpdateAsync(9999, updateDto);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_WithDuplicateEmail_ShouldReturnFalse()
        {
            await _fixture.ClearAsync();
            var user1 = TestDataGenerator.GenerateValidUser(1, "user1@example.com");
            var user2 = TestDataGenerator.GenerateValidUser(2, "user2@example.com");
            _fixture.Context.Users.AddRange(user1, user2);
            await _fixture.Context.SaveChangesAsync();
            var updateDto = new UpdateUserDto { Email = "user1@example.com" };
            var result = await _userService.UpdateAsync(2, updateDto);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldDeleteSuccessfully()
        {
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "delete@example.com");
            _fixture.Context.Users.Add(user);
            await _fixture.Context.SaveChangesAsync();
            var result = await _userService.DeleteAsync(user.Id);
            result.Should().BeTrue();
            var deletedUser = await _userService.GetByIdAsync(user.Id);
            deletedUser.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
        {
            await _fixture.ClearAsync();
            var result = await _userService.DeleteAsync(9999);
            result.Should().BeFalse();
        }
    }
}
