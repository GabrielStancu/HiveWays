using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;

namespace HiveWays.FleetIntegration;

public class TrafficBalancer
{
    private readonly ITrafficBalancerService _trafficBalancer;
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly ILogger<TrafficBalancer> _logger;

    public TrafficBalancer(ITrafficBalancerService trafficBalancer,
        IRedisClient<VehicleStats> redisClient,
        ILogger<TrafficBalancer> logger)
    {
        _trafficBalancer = trafficBalancer;
        _redisClient = redisClient;
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
        _logger.LogInformation("Fetched stats for vehicles: {FetchedVehicleStatsIds}", vehicleStats.Select(s => s.Id));

        var (mainRoadRatio, secondaryRoadRatio) = _trafficBalancer.UpdateBalancingRatio(congestedVehicles, vehicleStats);
        _logger.LogInformation("Recomputed main road ratio: {MainRoadRatio}; secondary road ratio: {SecondaryRoadRatio}", mainRoadRatio, secondaryRoadRatio);

        // TODO: Upload these values in some kind of table, maybe the balancer service should take the values from there
        // TODO: Build layer over the table from which the client can fetch periodically the enw value of the slider
    }
}