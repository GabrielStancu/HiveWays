using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration
{
    public class CachePolling
    {
        private readonly IRedisClient<LastKnownValue> _redisClient;
        private readonly ILogger _logger;

        public CachePolling(IRedisClient<LastKnownValue> redisClient,
            ILoggerFactory loggerFactory)
        {
            _redisClient = redisClient;
            _logger = loggerFactory.CreateLogger<CachePolling>();
        }

        [Function("CachePolling")]
        public async Task Run([TimerTrigger("* */1 * * * *")] TimerInfo myTimer)
        {
            var lastKnownValues = await _redisClient.GetElementsAsync();
            var count = lastKnownValues?.Count() ?? 0;

            if (count > 0)
            {
                _logger.LogWarning($"Fetched {count} items from last known values cache");
            }
        }
    }
}
