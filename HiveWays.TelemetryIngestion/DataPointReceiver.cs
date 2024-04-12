using System.Text.Json;
using Azure.Messaging.ServiceBus;
using HiveWays.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
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
    public async Task Run([ServiceBusTrigger("%CarEventsServiceBus:EventReceivedQueueName%", Connection = "CarEventsServiceBus:ConnectionString")] ServiceBusReceivedMessage message,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var dataPointsBatch = JsonSerializer.Deserialize<DataPointsBatch>(message.Body);
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(DataIngestionOrchestrator), dataPointsBatch);

        _logger.LogInformation("Finished ingestion pipeline run with instance id {IngestionPipelineInstanceId}", instanceId);
    }
}