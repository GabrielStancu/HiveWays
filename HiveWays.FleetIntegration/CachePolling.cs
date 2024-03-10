using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration
{
    public class CachePolling
    {
        private readonly IRedisClient<VehicleStats> _redisClient;
        private readonly IVehicleClustering _vehicleClustering;
        private readonly ILogger<CachePolling> _logger;

        public CachePolling(IRedisClient<VehicleStats> redisClient,
            IVehicleClustering vehicleClustering,
            ILogger<CachePolling> logger,
            ClusterConfiguration clusterConfiguration)
        {
            _redisClient = redisClient;
            _vehicleClustering = vehicleClustering;
            _logger = logger;
        }

        [Function("CachePolling")]
        public async Task Run([TimerTrigger("* */1 * * * *")] TimerInfo myTimer)
        {
            var vehicleStatsSets = await _redisClient.GetElementsAsync();
            var vehicleStats = vehicleStatsSets
                .GroupBy(v => v.Id)
                .Select(g => g.ToList())
                .Select(v => v[v.Count / 2]);
            var clusters = _vehicleClustering.KMeans(vehicleStats.ToList());

            _logger.LogInformation("Found clusters: {VehicleClusters}", JsonSerializer.Serialize(clusters));
        }
    }
}
