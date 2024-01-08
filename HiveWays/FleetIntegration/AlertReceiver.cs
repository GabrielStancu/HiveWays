using System;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FleetIntegration
{
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
            // This will process alert messages, storing directly in the data lake
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
        }
    }
}
