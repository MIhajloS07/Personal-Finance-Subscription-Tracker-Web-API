using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace Personal_Finance___Subscription_Tracker_API.Tests.Fixtures
{
    /// <summary>
    /// Provides mock Redis cache for testing 
    /// </summary>
    public class CacheFixture
    {
        public Mock<IDistributedCache> MockCache { get; }

        public CacheFixture()
        {
            MockCache = new Mock<IDistributedCache>();

            // Setup default behavior
            MockCache
                .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);
        }
    }
}
