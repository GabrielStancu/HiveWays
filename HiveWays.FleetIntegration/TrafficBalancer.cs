using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;
using HiveWays.VehicleEdge.Models;

namespace HiveWays.FleetIntegration;

public class TrafficBalancer
{
    private readonly ITrafficBalancerService _trafficBalancer;
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly IRoutingInfoTableClient _tableClient;
    private readonly RoadConfiguration _roadConfiguration;
    private readonly ICosmosDbClient<VehicleData> _cosmosDbClient;
    private readonly ILogger<TrafficBalancer> _logger;

    public TrafficBalancer(ITrafficBalancerService trafficBalancer,
        IRedisClient<VehicleStats> redisClient,
        IRoutingInfoTableClient tableClient,
        RoadConfiguration roadConfiguration,
        ICosmosDbClient<VehicleData> cosmosDbClient,
        ILogger<TrafficBalancer> logger)
    {
        _trafficBalancer = trafficBalancer;
        _redisClient = redisClient;
        _tableClient = tableClient;
        _roadConfiguration = roadConfiguration;
        _cosmosDbClient = cosmosDbClient;
        _logger = logger;
    }


    [Function(nameof(TrafficBalancer))]
    public async Task Run([TimerTrigger("* * * * * *")] TimerInfo myTimer)
    {
        var congestedVehicles = new List<CongestedVehicle>();
        var vehiclesData = await _cosmosDbClient.GetDocumentsAsync();
        var previousRatio = await _tableClient
            .GetEntityAsync(_roadConfiguration.MainRoadId.ToString(), _roadConfiguration.SecondaryRoadId.ToString());
        _logger.LogInformation("Fetched previous routing ratio: {PreviousRoutingRatio}", previousRatio.Value);

        var newRatio = _trafficBalancer.RecomputeBalancingRatio(congestedVehicles, vehiclesData.ToList(), previousRatio.Value);
        _logger.LogInformation("Recomputed main road ratio: {MainRoadRatio}", newRatio);

        var routingInfo = new RoutingInfoEntity
        {
            PartitionKey = _roadConfiguration.MainRoadId.ToString(),
            RowKey = _roadConfiguration.SecondaryRoadId.ToString(),
            Value = newRatio
        };

        await _tableClient.UpsertEntityAsync(routingInfo);
        _logger.LogInformation("Upserted new value of {MainRoadRatio} for roads with id {MainRoadId} and road with id {SecondaryRoadId}",
            routingInfo.Value, routingInfo.PartitionKey, routingInfo.RowKey);
    }
}