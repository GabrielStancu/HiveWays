using Azure.Messaging.ServiceBus;
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
        _logger.LogInformation("Starting ingestion pipeline...");

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(DataIngestionOrchestrator), message);

        _logger.LogInformation("Finished ingestion pipeline run with instance id {IngestionPipelineInstanceId}", instanceId);
    }
}