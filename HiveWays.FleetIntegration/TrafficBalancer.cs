using Azure.Messaging.ServiceBus;
using HiveWays.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace HiveWays.FleetIntegration;

public class TrafficBalancer
{
    private readonly ILogger<TrafficBalancer> _logger;

    public TrafficBalancer(ILogger<TrafficBalancer> logger)
    {
        _logger = logger;
    }


    [Function(nameof(TrafficBalancer))]
    public async Task Run([ServiceBusTrigger("%CongestionQueue:QueueName%", Connection = "CongestionQueue:ConnectionString")] ServiceBusReceivedMessage message,
        FunctionContext executionContext)
    {
        var messageBody = Encoding.UTF8.GetString(message.Body);
        _logger.LogInformation("Received vehicle stats: {ReceivedVehicleStats}", messageBody);

        var vehicleStats = JsonConvert.DeserializeObject<List<VehicleStats>>(messageBody);

        // TODO [G]
        // We should compute degree of congestion for each road
        // If main road too busy => increase value for alternative route taking
        // If secondary road too busy => decrease value for alternative route taking
        // If none too busy => slightly decrease alternative route taking value (main road is shorter and allows higher traffic)
    }
}