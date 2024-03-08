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
        private readonly ClusterConfiguration _clusterConfiguration;

        public CachePolling(IRedisClient<VehicleStats> redisClient,
            IVehicleClustering vehicleClustering,
            ILogger<CachePolling> logger,
            ClusterConfiguration clusterConfiguration)
        {
            _redisClient = redisClient;
            _vehicleClustering = vehicleClustering;
            _logger = logger;
            _clusterConfiguration = clusterConfiguration;
        }

        [Function("CachePolling")]
        public async Task Run([TimerTrigger("* */1 * * * *")] TimerInfo myTimer)
        {
            var vehicleStats = await _redisClient.GetElementsAsync();
            var clusters = _vehicleClustering.KMeans(vehicleStats.ToList(), _clusterConfiguration.ClustersCount);

            _logger.LogInformation("Found clusters: {VehicleClusters}", JsonSerializer.Serialize(clusters));
        }
    }
}
