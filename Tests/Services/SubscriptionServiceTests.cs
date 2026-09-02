using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Personal_Finance___Subscription_Tracker_API.DTOs.Subscription;
using Personal_Finance___Subscription_Tracker_API.Services.interfaces;
using Personal_Finance___Subscription_Tracker_API.Services.implementations;
using Personal_Finance___Subscription_Tracker_API.Tests.Fixtures;
using Personal_Finance___Subscription_Tracker_API.Tests.TestData;
using Xunit;

namespace Personal_Finance___Subscription_Tracker_API.Tests.Services
{
    /// <summary>
    /// Unit tests for SubscriptionService
    /// Tests: CRUD operations, filtering, and business logic
    /// </summary>
    public class SubscriptionServiceTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            var mockCache = new Mock<ICacheService>();
            _subscriptionService = new SubscriptionService(_fixture.Context, mockCache.Object);
        }

        #region Create Tests

        [Fact]
        public async Task CreateAsync_WithValidSubscription_ShouldCreateSuccessfully()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            _fixture.Context.Users.Add(user);
            await _fixture.Context.SaveChangesAsync();

            var createDto = new CreateSubscriptionDto
            {
                Name = "Netflix",
                Price = 15.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Entertainment",
                UserId = 1
            };

            // Act
            var result = await _subscriptionService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Netflix");
            result.Price.Should().Be(15.99m);
            result.UserId.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidUserId_ShouldReturnNull()
        {
            // Arrange
            await _fixture.ClearAsync();
            var createDto = new CreateSubscriptionDto
            {
                Name = "Netflix",
                Price = 15.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Entertainment",
                UserId = 9999 // Non-existent user
            };

            // Act
            var result = await _subscriptionService.CreateAsync(createDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_WithZeroPrice_ShouldStillCreate()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            _fixture.Context.Users.Add(user);
            await _fixture.Context.SaveChangesAsync();

            var createDto = new CreateSubscriptionDto
            {
                Name = "Free Service",
                Price = 0m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Entertainment",
                UserId = 1
            };

            // Act
            var result = await _subscriptionService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Price.Should().Be(0m);
        }

        #endregion

        #region Read Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSubscriptions()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            var subscriptions = TestDataGenerator.GenerateSubscriptions(3, 1);
            
            _fixture.Context.Users.Add(user);
            user.Subscriptions = subscriptions;
            await _fixture.Context.SaveChangesAsync();

            // Act
            var result = await _subscriptionService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnSubscription()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            var subscription = TestDataGenerator.GenerateValidSubscription(1, 1);
            
            _fixture.Context.Users.Add(user);
            _fixture.Context.Subscriptions.Add(subscription);
            await _fixture.Context.SaveChangesAsync();

            // Act
            var result = await _subscriptionService.GetByIdAsync(subscription.Id);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(subscription.Name);
            result.Id.Should().Be(subscription.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Act
            var result = await _subscriptionService.GetByIdAsync(9999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnUserSubscriptions()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            var subscriptions = TestDataGenerator.GenerateSubscriptions(3, 1);
            
            _fixture.Context.Users.Add(user);
            foreach (var sub in subscriptions)
            {
                _fixture.Context.Subscriptions.Add(sub);
            }
            await _fixture.Context.SaveChangesAsync();

            // Act
            var result = await _subscriptionService.GetByUserIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.All(s => s.UserId == 1).Should().BeTrue();
        }

        [Fact]
        public async Task GetByUserIdAsync_WithInvalidUserId_ShouldReturnNull()
        {
            // Act
            var result = await _subscriptionService.GetByUserIdAsync(9999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByNameAsync_ShouldReturnMatchingSubscriptions()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            var subscription = TestDataGenerator.GenerateValidSubscription(1, 1);
            
            _fixture.Context.Users.Add(user);
            _fixture.Context.Subscriptions.Add(subscription);
            await _fixture.Context.SaveChangesAsync();

            // Act
            var result = await _subscriptionService.GetByNameAsync("Netflix");

            // Assert
            result.Should().NotBeNull();
            result.All(s => s.Name.Contains("Netflix")).Should().BeTrue();
        }

        [Fact]
        public async Task GetByNameAsync_WithNonExistentName_ShouldReturnNull()
        {
            // Act
            var result = await _subscriptionService.GetByNameAsync("NonExistent");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task UpdateAsync_WithValidSubscription_ShouldUpdateSuccessfully()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            var subscription = TestDataGenerator.GenerateValidSubscription(1, 1);
            
            _fixture.Context.Users.Add(user);
            _fixture.Context.Subscriptions.Add(subscription);
            await _fixture.Context.SaveChangesAsync();

            var updateDto = new UpdateSubscriptionDto
            {
                Name = "Netflix Premium Plus",
                Price = 19.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow.AddDays(10),
                Category = "Entertainment",
                UserId = user.Id
            };

            // Act
            var result = await _subscriptionService.UpdateAsync(subscription.Id, updateDto);

            // Assert
            result.Should().BeTrue();
            var updated = await _subscriptionService.GetByIdAsync(subscription.Id);
            updated.Name.Should().Be("Netflix Premium Plus");
            updated.Price.Should().Be(19.99m);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            await _fixture.ClearAsync();
            var updateDto = new UpdateSubscriptionDto
            {
                Name = "Any",
                Price = 10m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow,
                Category = "Any"
            };

            // Act
            var result = await _subscriptionService.UpdateAsync(9999, updateDto);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldDeleteSuccessfully()
        {
            // Arrange
            await _fixture.ClearAsync();
            var user = TestDataGenerator.GenerateValidUser(1, "user@example.com");
            var subscription = TestDataGenerator.GenerateValidSubscription(1, 1);
            
            _fixture.Context.Users.Add(user);
            _fixture.Context.Subscriptions.Add(subscription);
            await _fixture.Context.SaveChangesAsync();

            // Act
            var result = await _subscriptionService.DeleteAsync(subscription.Id);

            // Assert
            result.Should().BeTrue();
            var deleted = await _subscriptionService.GetByIdAsync(subscription.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Act
            var result = await _subscriptionService.DeleteAsync(9999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
