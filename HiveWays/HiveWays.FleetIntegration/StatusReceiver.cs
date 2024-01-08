using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration;

public class StatusReceiver
{
    private readonly ILogger<StatusReceiver> _logger;

    public StatusReceiver(ILogger<StatusReceiver> logger)
    {
        _logger = logger;
    }

    [Function(nameof(StatusReceiver))]
    public void Run([ServiceBusTrigger("%TrafficServiceBus:StatusQueue%", Connection = "TrafficServiceBus:ConnectionString")] ServiceBusReceivedMessage message)
    {
        // This will process status messages, storing locally the last known values for each car, then storing all values in data lake
        _logger.LogInformation("Message ID: {id}", message.MessageId);
    }
}