using System.Collections.Concurrent;
using System.Text.Json;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Entities;
using HiveWays.Domain.Models;
using HiveWays.Infrastructure.Factories;
using HiveWays.TelemetryIngestion.Business;
using HiveWays.TelemetryIngestion.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace HiveWays.TelemetryIngestion;

public class DataIngestionOrchestrator
{
    private readonly IDataPointValidator _dataPointValidator;
    private readonly ITableStorageClient<DataPointEntity> _tableStorageClient;
    private readonly IServiceBusSenderFactory _serviceBusSenderFactory;
    private readonly IngestionConfiguration _ingestionConfiguration;
    private readonly RoutingServiceBusConfiguration _routingServiceBusConfiguration;
    private readonly IMessageRouter _messageRouter;
    private readonly ILogger<DataIngestionOrchestrator> _logger;
    
    private readonly DateTime _timeReference;

    public DataIngestionOrchestrator(
        IDataPointValidator dataPointValidator,
        ITableStorageClient<DataPointEntity> tableStorageClient,
        IServiceBusSenderFactory serviceBusSenderFactory,
        IngestionConfiguration ingestionConfiguration,
        RoutingServiceBusConfiguration routingServiceBusConfiguration,
        IMessageRouter messageRouter,
        ILogger<DataIngestionOrchestrator> logger)
    {
        _dataPointValidator = dataPointValidator;
        _tableStorageClient = tableStorageClient;
        _serviceBusSenderFactory = serviceBusSenderFactory;
        _ingestionConfiguration = ingestionConfiguration;
        _routingServiceBusConfiguration = routingServiceBusConfiguration;
        _messageRouter = messageRouter;
        _logger = logger;
        _timeReference = DateTime.UtcNow;
    }

    [Function(nameof(DataIngestionOrchestrator))]
    public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var inputDataPoints = context.GetInput<IEnumerable<DataPoint>>().ToList();
        var validationResults = await ValidateDataPointsAsync(context, inputDataPoints);
        var validDataPointEntities = await EnrichDataPointsAsync(context, inputDataPoints, validationResults);
        var storedDataPointsBatch = await context.CallActivityAsync<bool>(nameof(StoreEnrichedDataPoints), validDataPointEntities);

        if (storedDataPointsBatch)
        {
            await context.CallActivityAsync(nameof(RouteMessages), validDataPointEntities);
        }
    }

    private async Task<List<bool>> ValidateDataPointsAsync(TaskOrchestrationContext context, List<DataPoint> inputDataPoints)
    {
        var validationResults = await context.CallActivityAsync<List<bool>>(nameof(ValidateDataPoints), inputDataPoints);
        if (inputDataPoints.Count != validationResults.Count)
        {
            string errorMessage = "Input length different from validation results. Aborting further processing";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        return validationResults;
    }

    private async Task<IEnumerable<DataPointEntity>> EnrichDataPointsAsync(TaskOrchestrationContext context, List<DataPoint> inputDataPoints, List<bool> validationResults)
    {
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

        var validDataPointEntities =
            await context.CallActivityAsync<IEnumerable<DataPointEntity>>(nameof(EnrichDataPoints), validDataPoints);
        return validDataPointEntities;
    }

    [Function(nameof(ValidateDataPoints))]
    public async Task<List<bool>> ValidateDataPoints([ActivityTrigger] IEnumerable<DataPoint> dataPoints)
    {
        var validationResults = new ConcurrentDictionary<DataPoint, bool>();

        Parallel.ForEach(dataPoints, dataPoint =>
        {
            var validationResult = _dataPointValidator.ValidateDataPointRange(dataPoint);
            validationResults.TryAdd(validationResult.Key, validationResult.Value);
        });

        await _dataPointValidator.CheckDevicesRegistrationAsync(validationResults);

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
    public async Task<bool> StoreEnrichedDataPoints([ActivityTrigger] IEnumerable<DataPointEntity> dataPointEntities)
    {
        try
        {
            await _tableStorageClient.AddEntitiesBatchedAsync(dataPointEntities);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception while adding batch to table storage: {TableStorageAddException} @ {TableStorageAddStackTrace}", ex.Message, ex.StackTrace);
            return false;
        }
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
        var queue = _messageRouter.GetRoutingQueue(messageType);
        var sender = _serviceBusSenderFactory.GetServiceBusSenderClient(_routingServiceBusConfiguration.ConnectionString, queue);

        await sender.SendMessageAsync(message);
    }
}