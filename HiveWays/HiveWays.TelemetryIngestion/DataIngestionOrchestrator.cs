using System.Collections.Concurrent;
using System.Text.Json;
using HiveWays.Business.CosmosDbClient;
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
    public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var inputDataPoints = context.GetInput<IEnumerable<DataPoint>>().ToList();

        // Validate data points
        var validationResults = await context.CallActivityAsync<List<bool>>(nameof(ValidateDataPoints), inputDataPoints);
        if (inputDataPoints.Count != validationResults.Count)
        {
            string errorMessage = "Input length different from validation results. Aborting further processing";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Enrich data points
        var validDataPoints = new ConcurrentBag<DataPoint>();

        Parallel.For(0, inputDataPoints.Count, idx =>
        {
            if (validationResults[idx])
            {
                validDataPoints.Add(inputDataPoints[idx]);
            }
            else
            {
                _logger.LogError("Validation for data point {FailedValidationDataPoint} failed",
                    JsonSerializer.Serialize(inputDataPoints[idx]));
            }
        });

        var validDataPointEntities = await context.CallActivityAsync<IEnumerable<DataPointEntity>>(nameof(EnrichDataPoints), validDataPoints);

        // Store and route validated points
        await context.CallActivityAsync(nameof(StoreEnrichedDataPoints), validDataPointEntities);
        await context.CallActivityAsync(nameof(RouteMessages), validDataPointEntities);
    }

    [Function(nameof(ValidateDataPoints))]
    public async Task<List<bool>> ValidateDataPoints([ActivityTrigger] IEnumerable<DataPoint> dataPoints)
    {
        var validationResults = new ConcurrentDictionary<DataPoint, bool>();

        Parallel.ForEach(dataPoints, dataPoint =>
        {
            var validationResult = ValidateDataPointRange(dataPoint);
            validationResults.TryAdd(validationResult.Key, validationResult.Value);
        });

        await CheckDevicesRegistrationAsync(validationResults);

        return validationResults.Values.ToList();
    }

    [Function(nameof(EnrichDataPoints))]
    public IEnumerable<DataPointEntity> EnrichDataPoints([ActivityTrigger] IEnumerable<DataPoint> dataPoints)
    {
        var entities = new List<DataPointEntity>();

        Parallel.ForEach(dataPoints, dataPoint =>
        {
            var dataPointEntity = new DataPointEntity
            {
                PartitionKey = dataPoint.Id.ToString(),
                RowKey = _timeReference.AddSeconds(dataPoint.TimeOffsetSeconds)
                    .ToString("o"), // TODO: make the simulator generate timestamps properly
                SpeedMps = dataPoint.Speed,
                SpeedKmph = dataPoint.Speed * 3.6m,
                AccelerationMps = dataPoint.Acceleration,
                AccelerationKmph = dataPoint.Acceleration * 12960,
                Latitude = _ingestionConfiguration.ReferenceLatitude + dataPoint.Y,
                Longitude = _ingestionConfiguration.ReferenceLongitude + dataPoint.X,
                Heading = dataPoint.Heading
            };
            entities.Add(dataPointEntity);
        });

        return entities;
    }

    [Function(nameof(StoreEnrichedDataPoints))]
    public async Task StoreEnrichedDataPoints([ActivityTrigger] IEnumerable<DataPointEntity> dataPointEntities)
    {
        await _tableStorageClient.UpsertEntitiesBatchedAsync(dataPointEntities);
    }

    [Function(nameof(RouteMessages))]
    public async Task RouteMessages([ActivityTrigger] IEnumerable<DataPointEntity> dataPointEntities)
    {
        var message = dataPointEntities.Select(e => new TrafficMessage
        {
            DeviceId = e.PartitionKey,
            Timestamp = DateTime.Parse(e.RowKey),
            SpeedMps = e.SpeedMps,
            SpeedKmph = e.SpeedKmph,
            AccelerationMps = e.AccelerationMps,
            AccelerationKmph = e.AccelerationKmph,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            Heading = e.Heading
        });
        var messageType = ServiceBusMessageType.StatusReceived; // TODO: get this dynamically, if alert or traffic or trip
        var queue = GetRoutingQueue(messageType);
        var sender = _serviceBusSenderFactory.GetServiceBusSenderClient(_routingServiceBusConfiguration.ConnectionString, queue);

        await sender.SendMessageAsync(message);
    }

    private KeyValuePair<DataPoint, bool> ValidateDataPointRange(DataPoint dataPoint)
    {
        if (dataPoint.Id < _ingestionConfiguration.MinId || dataPoint.Id > _ingestionConfiguration.MaxId)
        {
            _logger.LogError("Item is not registered, id: {UnregisteredItemId}", dataPoint.Id);
            return new KeyValuePair<DataPoint, bool>(dataPoint, false);
        }

        if (dataPoint.Speed < _ingestionConfiguration.MinSpeed || dataPoint.Speed > _ingestionConfiguration.MaxSpeed)
        {
            _logger.LogError("Invalid speed indicating error data: {InvalidSpeed} m/s", dataPoint.Speed);
            return new KeyValuePair<DataPoint, bool>(dataPoint, false);
        }

        return new KeyValuePair<DataPoint, bool>(dataPoint, true);
    }

    private async Task CheckDevicesRegistrationAsync(ConcurrentDictionary<DataPoint, bool> validationResults)
    {
        var validIds = validationResults
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key.Id)
            .Distinct()
            .ToList();
        var registrationResults = await AreIdsRegisteredAsync(validIds);

        Parallel.ForEach(validationResults.Where(kvp => kvp.Value), validationResult =>
        {
            var id = validationResult.Key.Id;
            if (registrationResults.Keys.Contains(id))
            {
                validationResults[validationResult.Key] = registrationResults[id];
            }
        });
    }

    private async Task<Dictionary<int, bool>> AreIdsRegisteredAsync(List<int> ids)
    {
        var registrationResults = new Dictionary<int, bool>();
        foreach (var id in ids)
        {
            registrationResults.Add(id, false);
        }

        try
        {
            var registeredDevices = (await _itemCosmosClient.GetDocumentsByQueryAsync(devices =>
            {
                var filteredDevices = devices.Where(d => ids.Contains(d.ExternalId));
                return filteredDevices as IOrderedQueryable<BaseDevice>;
            })).ToList();

            foreach (var id in ids)
            {
                var isRegisteredDevice = registeredDevices.FirstOrDefault(rd => rd.ExternalId == id) != null;
                if (!isRegisteredDevice)
                {
                    _logger.LogError("Item with id could not be found as registered item: {UnregisteredItemId}", id);
                }
                registrationResults[id] = isRegisteredDevice;
            }

            return registrationResults;
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered exception while validating devices: {ValidationExceptionMessage} @ {ValidationExceptionStackTrace}", ex.Message, ex.StackTrace);
            return registrationResults;
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