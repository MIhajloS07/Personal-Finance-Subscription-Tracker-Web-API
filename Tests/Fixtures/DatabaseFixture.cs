using Microsoft.EntityFrameworkCore;
using Personal_Finance___Subscription_Tracker_API.Data;

namespace Personal_Finance___Subscription_Tracker_API.Tests.Fixtures
{
    /// <summary>
    /// Provides in-memory database context for integration testing
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        private readonly string _databaseName;
        public AppDbContext Context { get; private set; }

        public DatabaseFixture()
        {
            _databaseName = $"test_db_{Guid.NewGuid()}";
            Context = CreateContext();
            Context.Database.EnsureCreated();
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;
            return new AppDbContext(options);
        }

        public async Task ClearAsync()
        {
            Context.Subscriptions.RemoveRange(Context.Subscriptions);
            Context.Users.RemoveRange(Context.Users);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
