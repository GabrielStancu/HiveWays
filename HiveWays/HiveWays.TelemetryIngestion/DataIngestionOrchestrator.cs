using System.Text.Json;
using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Documents;
using HiveWays.Domain.Entities;
using HiveWays.Domain.Models;
using HiveWays.Infrastructure.Factories;
using HiveWays.TelemetryIngestion.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace HiveWays.TelemetryIngestion;

public class DataIngestionOrchestrator
{
    private readonly ICosmosDbClient<BaseDevice> _itemCosmosClient;
    private readonly ITableStorageClient<DataPointEntity> _tableStorageClient;
    private readonly IServiceBusSenderFactory _serviceBusSenderFactory;
    private readonly IngestionConfiguration _ingestionConfiguration;
    private readonly RoutingServiceBusConfiguration _routingServiceBusConfiguration;
    private readonly ILogger<DataIngestionOrchestrator> _logger;
    private readonly DateTime _timeReference;

    public DataIngestionOrchestrator(
        ICosmosDbClient<BaseDevice> itemCosmosClient,
        ITableStorageClient<DataPointEntity> tableStorageClient,
        IServiceBusSenderFactory serviceBusSenderFactory,
        IngestionConfiguration ingestionConfiguration,
        RoutingServiceBusConfiguration routingServiceBusConfiguration,
        ILogger<DataIngestionOrchestrator> logger)
    {
        _itemCosmosClient = itemCosmosClient;
        _tableStorageClient = tableStorageClient;
        _serviceBusSenderFactory = serviceBusSenderFactory;
        _ingestionConfiguration = ingestionConfiguration;
        _routingServiceBusConfiguration = routingServiceBusConfiguration;
        _logger = logger;
        _timeReference = DateTime.UtcNow;
    }

    [Function(nameof(DataIngestionOrchestrator))]
    public async Task<bool> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        _logger.LogInformation("Starting ingestion pipeline...");

        var inputDataPoint = context.GetInput<DataPoint>();

        _logger.LogInformation("Received input data point {InputDataPoint}", JsonSerializer.Serialize(inputDataPoint));

        var isValidInput = await context.CallActivityAsync<bool>(nameof(ValidateDataPoint), inputDataPoint);

        if (!isValidInput)
            return false;

        var dataPointEntity = await context.CallActivityAsync<DataPointEntity>(nameof(EnrichDataPoint), inputDataPoint);

        await context.CallActivityAsync(nameof(StoreEnrichedDataPoint), dataPointEntity);
        await context.CallActivityAsync(nameof(RouteMessage), dataPointEntity);

        return true;
    }

    [Function(nameof(ValidateDataPoint))]
    public async Task<bool> ValidateDataPoint([ActivityTrigger] DataPoint dataPoint)
    {
        if (dataPoint.Id < _ingestionConfiguration.MinId || dataPoint.Id > _ingestionConfiguration.MaxId)
        {
            _logger.LogError("Item is not registered, id: {UnregisteredItemId}", dataPoint.Id);
            return false;
        }

        if (dataPoint.Speed < _ingestionConfiguration.MinSpeed || dataPoint.Speed > _ingestionConfiguration.MaxSpeed)
        {
            _logger.LogError("Invalid speed indicating error data: {InvalidSpeed} m/s", dataPoint.Speed);
            return false;
        }

        return await IsItemRegisteredAsync(dataPoint.Id);
    }

    [Function(nameof(EnrichDataPoint))]
    public DataPointEntity EnrichDataPoint([ActivityTrigger] DataPoint dataPoint)
    {
        var dataPointEntity = new DataPointEntity
        {
            PartitionKey = dataPoint.Id.ToString(),
            RowKey = _timeReference.AddSeconds(dataPoint.TimeOffsetSeconds).ToString("o"), // TODO: make the simulator generate timestamps properly
            SpeedMps = dataPoint.Speed,
            SpeedKmph = dataPoint.Speed * 3.6m,
            AccelerationMps = dataPoint.Acceleration,
            AccelerationKmph = dataPoint.Acceleration * 12960,
            Latitude = _ingestionConfiguration.ReferenceLatitude + dataPoint.Y,
            Longitude = _ingestionConfiguration.ReferenceLongitude + dataPoint.X,
            Heading = dataPoint.Heading
        };

        _logger.LogInformation("Enriched data point to {EnrichedDataPoint}", JsonSerializer.Serialize(dataPointEntity));

        return dataPointEntity;
    }

    [Function(nameof(StoreEnrichedDataPoint))]
    public async Task StoreEnrichedDataPoint([ActivityTrigger] DataPointEntity dataPointEntity)
    {
        _logger.LogInformation("Storing data point entity {DataPointEntityToBeStored}", JsonSerializer.Serialize(dataPointEntity));
        await _tableStorageClient.UpsertEntityAsync(dataPointEntity);
    }

    [Function(nameof(RouteMessage))]
    public async Task RouteMessage([ActivityTrigger] DataPointEntity dataPointEntity)
    {
        var message = new TrafficMessage
        {
            DeviceId = dataPointEntity.PartitionKey,
            Timestamp = DateTime.Parse(dataPointEntity.RowKey),
            SpeedMps = dataPointEntity.SpeedMps,
            SpeedKmph = dataPointEntity.SpeedKmph,
            AccelerationMps = dataPointEntity.AccelerationMps,
            AccelerationKmph = dataPointEntity.AccelerationKmph,
            Latitude = dataPointEntity.Latitude,
            Longitude = dataPointEntity.Longitude,
            Heading = dataPointEntity.Heading
        };
        var messageType = ServiceBusMessageType.StatusReceived; // TODO: get this dynamically, if alert or traffic or trip
        var queue = GetRoutingQueue(messageType);
        var sender = _serviceBusSenderFactory.GetServiceBusSenderClient(_routingServiceBusConfiguration.ConnectionString, queue);

        _logger.LogInformation("Routing message {MessageToRoute} to queue {QueueRoute}", JsonSerializer.Serialize(message), queue);
        await sender.SendMessageAsync(message);
    }

    private async Task<bool> IsItemRegisteredAsync(int id)
    {
        try
        {
            var registeredItems = await _itemCosmosClient.GetDocumentsByQueryAsync(i => i.ExternalId == id);
            var isRegisteredItem = registeredItems.Single() != null;

            if (!isRegisteredItem)
            {
                _logger.LogError("Item with id could not be found as registered item: {UnregisteredItemId}", id);
            }

            return isRegisteredItem;
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered exception while validating device: {ValidationExceptionMessage} @ {ValidationExceptionStackTrace}", ex.Message, ex.StackTrace);
            return false;
        }
    }

    private string GetRoutingQueue(ServiceBusMessageType messageType)
    {
        switch (messageType)
        {
            case ServiceBusMessageType.AlertReceived:
                return _routingServiceBusConfiguration.AlertQueueName;
            case ServiceBusMessageType.StatusReceived:
                return _routingServiceBusConfiguration.StatusQueueName;
            case ServiceBusMessageType.TripReceived:
                return _routingServiceBusConfiguration.TripQueueName;
            default:
                throw new NotImplementedException($"Message of type {messageType} is not supported");
        }
    }
}