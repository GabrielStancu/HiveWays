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
        private readonly IVehicleClusterManager _vehicleClusterManager;
        private readonly ILogger<CachePolling> _logger;

        public CachePolling(IRedisClient<VehicleStats> redisClient,
            IVehicleClusterManager vehicleClusterManager,
            ILogger<CachePolling> logger,
            ClusterConfiguration clusterConfiguration)
        {
            _redisClient = redisClient;
            _vehicleClusterManager = vehicleClusterManager;
            _logger = logger;
        }

        [Function("CachePolling")]
        public async Task Run([TimerTrigger("* */1 * * * *")] TimerInfo myTimer)
        {
            var vehicleStatsSets = await _redisClient.GetElementsAsync();
            var vehicles = vehicleStatsSets
                .GroupBy(v => v.Id)
                .Select(g => new Vehicle
                {
                    Id = g.Key,
                    Info = g.Select(s => new VehicleInfo
                    {
                        Location = new GeoPoint
                        {
                            Latitude = s.Latitude,
                            Longitude = s.Longitude
                        },
                        Heading = s.Heading,
                        AccelerationKmph = s.AccelerationKmph,
                        SpeedKmph = s.SpeedKmph,
                        Timestamp = s.Timestamp
                    }).ToList()
                }).ToList();

            _logger.LogInformation("Clustering vehicles: {VehiclesToCluster}", JsonSerializer.Serialize(vehicles.Select(v => v.Id)));

            var clusters = await _vehicleClusterManager.ClusterVehiclesAsync(vehicles);

            _logger.LogInformation("Found clusters: {VehicleClusters}", JsonSerializer.Serialize(clusters));
        }
    }
}
