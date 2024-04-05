using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration;

public class ClusterVehicles
{
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly IVehicleClusterManager _vehicleClusterManager;
    private readonly ICongestionCalculator _congestionCalculator;
    private readonly ILogger<ClusterVehicles> _logger;

    public ClusterVehicles(IRedisClient<VehicleStats> redisClient,
        IVehicleClusterManager vehicleClusterManager,
        ICongestionCalculator congestionCalculator,
        ILogger<ClusterVehicles> logger)
    {
        _redisClient = redisClient;
        _vehicleClusterManager = vehicleClusterManager;
        _congestionCalculator = congestionCalculator;
        _logger = logger;
    }

    [Function(nameof(ClusterVehicles))]
    public async Task Run([TimerTrigger("* */1 * * * *")] TimerInfo myTimer)
    {
        var vehicleStatsSets = await _redisClient.GetElementsAsync();
        var vehicles = vehicleStatsSets
            .GroupBy(v => v.Id)
            .Select(MapStatsToVehicle)
            .ToList();

        _logger.LogInformation("Clustering vehicles: {VehiclesToCluster}", JsonSerializer.Serialize(vehicles.Select(v => v.Id)));

        var clusters = await _vehicleClusterManager.ClusterVehiclesAsync(vehicles);
        _logger.LogInformation("Found clusters: {VehicleClusters}", JsonSerializer.Serialize(clusters));

        var congestedClusters = _congestionCalculator.ComputeCongestedClusters(clusters);
        _logger.LogInformation("Found congested clusters: {VehicleClusters}", JsonSerializer.Serialize(congestedClusters));
    }

    private static Vehicle MapStatsToVehicle(IGrouping<int, VehicleStats> g) =>
        new()
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
        };
}