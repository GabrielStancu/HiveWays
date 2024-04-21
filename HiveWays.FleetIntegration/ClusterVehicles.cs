using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;
using HiveWays.Infrastructure.Factories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration;

public class ClusterVehicles
{
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly IVehicleClusterManager _vehicleClusterManager;
    private readonly ICongestionCalculator _congestionCalculator;
    private readonly IServiceBusSenderClient _sbClient;
    private readonly ILogger<ClusterVehicles> _logger;

    public ClusterVehicles(IRedisClient<VehicleStats> redisClient,
        IVehicleClusterManager vehicleClusterManager,
        ICongestionCalculator congestionCalculator,
        IServiceBusSenderFactory serviceBusSenderFactory,
        CongestionQueueConfiguration congestionQueueConfiguration,
        ILogger<ClusterVehicles> logger)
    {
        _redisClient = redisClient;
        _vehicleClusterManager = vehicleClusterManager;
        _congestionCalculator = congestionCalculator;
        _sbClient = serviceBusSenderFactory.GetServiceBusSenderClient(congestionQueueConfiguration.ConnectionString, congestionQueueConfiguration.QueueName);
        _logger = logger;
    }

    [Function(nameof(ClusterVehicles))]
    public async Task Run([TimerTrigger("* * * * * *")] TimerInfo myTimer)
    {
        var vehicleStatsSets = await _redisClient.GetElementsAsync();
        var vehicles = vehicleStatsSets
            .GroupBy(v => v.Id)
            .Select(MapStatsToVehicle)
            .ToList();

        _logger.LogInformation("Clustering vehicles: {VehiclesToCluster}", JsonSerializer.Serialize(vehicles.Select(v => v.Id)));

        var clusters = _vehicleClusterManager.ClusterVehicles(vehicles);
        _logger.LogInformation("Found clusters: {VehicleClusters}", JsonSerializer.Serialize(clusters));

        var congestedClusters = _congestionCalculator
            .ComputeCongestedClusters(clusters)
            .ToList();

        _logger.LogInformation("Found congested clusters: [{VehicleClusters}]", congestedClusters.Select(c => c.Id));

        var congestedVehicles = congestedClusters
            .SelectMany(c => c.Vehicles.Select(v => new CongestedVehicle
            {
                Id = v.Id,
                VehicleInfo = v.MedianInfo
            }))
            .ToList();

        _logger.LogInformation("Sending congestion data to traffic balancer {SentVehicleIds}", congestedVehicles.Select(v => v.Id));

        await _sbClient.SendMessageAsync(congestedVehicles);
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
                Timestamp = s.Timestamp,
                RoadId = s.RoadId
            })
            .OrderBy(i => i.Timestamp)
            .ToList()
        };
}