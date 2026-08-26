using Microsoft.EntityFrameworkCore;
using Personal_Finance___Subscription_Tracker_API.Model;

namespace Personal_Finance___Subscription_Tracker_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // representing models 'User' and 'Subscription' like table
        public DbSet<User> Users => Set<User>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
    }
}
