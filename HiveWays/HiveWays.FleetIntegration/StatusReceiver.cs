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
        // This will process status messages
        // Storing locally the last known values for each car (caching)
        // Then storing all values in data lake (table storage or cosmos)
        _logger.LogInformation("Message ID: {id}", message.MessageId);
    }
}