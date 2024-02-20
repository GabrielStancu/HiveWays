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
        public void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            //var lastKnownValues = await _redisClient.GetListElementsAsync()

            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus?.Next}");
        }
    }
}
