using System.Text.Json;
using HiveWays.Business.CosmosDbClient;
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

public class CongestionDetector
{
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly IVehicleClusterService _vehicleClusterService;
    private readonly ICongestionDetectionService _congestionDetectionService;
    private readonly ICosmosDbClient<ClusteringResult> _cosmosDbClient;
    private readonly IServiceBusSenderClient _sbClient;
    private readonly ILogger<CongestionDetector> _logger;

    public CongestionDetector(IRedisClient<VehicleStats> redisClient,
        IVehicleClusterService vehicleClusterService,
        ICongestionDetectionService congestionDetectionService,
        IServiceBusSenderFactory serviceBusSenderFactory,
        ICosmosDbClient<ClusteringResult> cosmosDbClient,
        CongestionQueueConfiguration congestionQueueConfiguration,
        ILogger<CongestionDetector> logger)
    {
        _redisClient = redisClient;
        _vehicleClusterService = vehicleClusterService;
        _congestionDetectionService = congestionDetectionService;
        _cosmosDbClient = cosmosDbClient;
        _sbClient = serviceBusSenderFactory.GetServiceBusSenderClient(congestionQueueConfiguration.ConnectionString, congestionQueueConfiguration.QueueName);
        _logger = logger;
    }

    [Function(nameof(CongestionDetector))]
    public async Task Run([TimerTrigger("* * * * * *")] TimerInfo myTimer)
    {
        var vehicleStatsSets = await _redisClient.GetElementsAsync();
        var vehicles = vehicleStatsSets
            .GroupBy(v => v.Id)
            .Select(MapStatsToVehicle)
            .ToList();

        _logger.LogInformation("Clustering vehicles: {VehiclesToCluster}", JsonSerializer.Serialize(vehicles.Select(v => v.Id)));

        var clusters = _vehicleClusterService.ClusterVehicles(vehicles);
        _logger.LogInformation("Found clusters: {VehicleClusters}", JsonSerializer.Serialize(clusters));

        var congestedClusters = _congestionDetectionService
            .ComputeCongestedClusters(clusters)
            .ToList();

        _logger.LogInformation("Found congested clusters: [{VehicleClusters}]", congestedClusters.Select(c => c.Id));

        var congestedVehicles = congestedClusters
            .SelectMany(c => c.Vehicles.Select(v => new CongestedVehicle
            {
                Id = v.Id,
                VehicleLocation = v.MedianLocation
            }))
            .ToList();

        _logger.LogInformation("Sending congestion data to traffic balancer {SentVehicleIds}", congestedVehicles.Select(v => v.Id));

        await _sbClient.SendMessageAsync(congestedVehicles);

        var clusteringResult = new ClusteringResult
        {
            Id = "routing-clusters",
            Clusters = clusters,
            CongestedClusters = congestedClusters
        };
        await _cosmosDbClient.UpsertDocumentAsync(clusteringResult);
    }

    private static Vehicle MapStatsToVehicle(IGrouping<int, VehicleStats> g) =>
        new()
        {
            Id = g.Key,
            Trajectory = g.Select(s => new VehicleLocation
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