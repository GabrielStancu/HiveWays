using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.TelemetryIngestion;

public class DataPointReceiver
{
    private readonly ILogger<DataPointReceiver> _logger;

    public DataPointReceiver(ILogger<DataPointReceiver> logger)
    {
        _logger = logger;
    }

    [Function(nameof(DataPointReceiver))]
    public void Run([ServiceBusTrigger("%CarInfoServiceBus:QueueName%", Connection = "CarInfoServiceBus:ConnectionString")] ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
    }
}