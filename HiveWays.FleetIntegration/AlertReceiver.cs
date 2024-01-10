using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration;

public class AlertReceiver
{
    private readonly ILogger<AlertReceiver> _logger;

    public AlertReceiver(ILogger<AlertReceiver> logger)
    {
        _logger = logger;
    }

    [Function(nameof(AlertReceiver))]
    public void Run([ServiceBusTrigger("%TrafficServiceBus:AlertQueue%", Connection = "TrafficServiceBus:ConnectionString")] ServiceBusReceivedMessage message)
    {
        // This will process alert messages
        // Storing directly in the data lake (table storage or cosmos)
        _logger.LogInformation("Message ID: {id}", message.MessageId);
    }
}