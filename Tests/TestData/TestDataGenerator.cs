using Personal_Finance___Subscription_Tracker_API.Model;

namespace Personal_Finance___Subscription_Tracker_API.Tests.TestData
{
    /// <summary>
    /// Generates test data for unit tests
    /// </summary>
    public static class TestDataGenerator
    {
        public static User GenerateValidUser(int id = 1, string email = "test@example.com")
        {
            return new User
            {
                Id = id,
                Email = email,
                PasswordHash = "hashed_password_123",
                Subscriptions = new List<Subscription>()
            };
        }

        public static Subscription GenerateValidSubscription(int id = 1, int userId = 1)
        {
            return new Subscription
            {
                Id = id,
                Name = "Netflix Premium",
                Price = 15.99m,
                Currency = "USD",
                PaymentDate = DateTime.UtcNow.AddDays(5),
                Category = "Entertainment",
                UserId = userId
            };
        }

        public static List<User> GenerateUsers(int count = 5)
        {
            var users = new List<User>();
            for (int i = 1; i <= count; i++)
            {
                users.Add(GenerateValidUser(i, $"user{i}@example.com"));
            }
            return users;
        }

        public static List<Subscription> GenerateSubscriptions(int count = 5, int userId = 1)
        {
            var subscriptions = new List<Subscription>();
            string[] names = { "Netflix", "GitHub Pro", "AWS", "Adobe", "Spotify", "OneDrive", "Slack" };
            string[] categories = { "Entertainment", "Development", "Cloud", "Design", "Music", "Storage", "Communication" };

            for (int i = 1; i <= count; i++)
            {
                subscriptions.Add(new Subscription
                {
                    Id = i,
                    Name = names[(i - 1) % names.Length],
                    Price = 10.00m + (i * 5),
                    Currency = "USD",
                    PaymentDate = DateTime.UtcNow.AddDays(i),
                    Category = categories[(i - 1) % categories.Length],
                    UserId = userId
                });
            }
            return subscriptions;
        }
    }
}
