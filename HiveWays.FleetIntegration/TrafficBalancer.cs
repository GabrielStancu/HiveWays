using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;

namespace HiveWays.FleetIntegration;

public class TrafficBalancer
{
    private readonly ITrafficBalancerService _trafficBalancer;
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly ITableStorageClient<RoutingInfoEntity> _tableClient;
    private readonly RoadConfiguration _roadConfiguration;
    private readonly ILogger<TrafficBalancer> _logger;

    public TrafficBalancer(ITrafficBalancerService trafficBalancer,
        IRedisClient<VehicleStats> redisClient,
        ITableStorageClient<RoutingInfoEntity> tableClient,
        RoadConfiguration roadConfiguration,
        ILogger<TrafficBalancer> logger)
    {
        _trafficBalancer = trafficBalancer;
        _redisClient = redisClient;
        _tableClient = tableClient;
        _roadConfiguration = roadConfiguration;
        _logger = logger;
    }


    [Function(nameof(TrafficBalancer))]
    public async Task Run([ServiceBusTrigger("%CongestionQueue:QueueName%", Connection = "CongestionQueue:ConnectionString")] ServiceBusReceivedMessage message,
        FunctionContext executionContext)
    {
        var messageBody = Encoding.UTF8.GetString(message.Body);
        _logger.LogInformation("Received vehicle stats: {ReceivedVehicleStats}", messageBody);
        var congestedVehicles = JsonSerializer.Deserialize<List<CongestedVehicle>>(messageBody);

        var vehicleStats = (await _redisClient.GetElementsAsync()).ToList();
        _logger.LogInformation("Fetched stats for vehicles: [{FetchedVehicleStatsIds}]", vehicleStats.Select(s => s.Id));

        var mainRoadRatio = _trafficBalancer.RecomputeBalancingRatio(congestedVehicles, vehicleStats);
        _logger.LogInformation("Recomputed main road ratio: {MainRoadRatio}", mainRoadRatio);

        var routingInfo = new RoutingInfoEntity
        {
            PartitionKey = _roadConfiguration.MainRoadId.ToString(),
            RowKey = _roadConfiguration.SecondaryRoadId.ToString(),
            Value = mainRoadRatio
        };

        await _tableClient.UpsertEntityAsync(routingInfo);
        _logger.LogInformation("Upserted new value of {MainRoadRatio} for roads with id {MainRoadId} and road with id {SecondaryRoadId}",
            routingInfo.Value, routingInfo.PartitionKey, routingInfo.RowKey);
    }
}