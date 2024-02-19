using System.Collections.Concurrent;
using HiveWays.Business.RedisClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Entities;
using HiveWays.Domain.Models;
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
    private readonly IRedisClient<LastKnownValue> _redisClient;
    private readonly IngestionConfiguration _ingestionConfiguration;
    private readonly ILogger<DataIngestionOrchestrator> _logger;
    
    private readonly DateTime _timeReference;

    public DataIngestionOrchestrator(
        IDataPointValidator dataPointValidator,
        ITableStorageClient<DataPointEntity> tableStorageClient,
        IRedisClient<LastKnownValue> redisClient,
        IngestionConfiguration ingestionConfiguration,
        ILogger<DataIngestionOrchestrator> logger)
    {
        _dataPointValidator = dataPointValidator;
        _tableStorageClient = tableStorageClient;
        _redisClient = redisClient;
        _ingestionConfiguration = ingestionConfiguration;
        _logger = logger;
        _timeReference = DateTime.UtcNow;
    }

    [Function(nameof(DataIngestionOrchestrator))]
    public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var inputDataPoints = context.GetInput<IEnumerable<DataPoint>>().ToList();
        var validDataPoints = await ValidateDataPointsAsync(context, inputDataPoints);

        var lastKnownValues = MapDataPointsToKnownValues(validDataPoints);
        await context.CallActivityAsync(nameof(StoreLastKnownValues), lastKnownValues);

        var validDataPointEntities = await context.CallActivityAsync<IEnumerable<DataPointEntity>>(nameof(EnrichDataPoints), validDataPoints);
        await context.CallActivityAsync(nameof(StoreHistoricalData), validDataPointEntities);
    }

    private async Task<List<DataPoint>> ValidateDataPointsAsync(TaskOrchestrationContext context, List<DataPoint> inputDataPoints)
    {
        var validationResults = await context.CallActivityAsync<Dictionary<DataPoint, bool>>(nameof(ValidateDataPoints), inputDataPoints);
        if (inputDataPoints.Count != validationResults.Count)
        {
            string errorMessage = "Input length different from validation results. Aborting further processing";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var validDataPoints = validationResults
            .Where(vr => vr.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        return validDataPoints;
    }

    [Function(nameof(ValidateDataPoints))]
    public async Task<Dictionary<DataPoint, bool>> ValidateDataPoints([ActivityTrigger] IEnumerable<DataPoint> dataPoints)
    {
        var validationResults = new ConcurrentDictionary<DataPoint, bool>();

        Parallel.ForEach(dataPoints, dataPoint =>
        {
            var validationResult = _dataPointValidator.ValidateDataPointRange(dataPoint);
            validationResults.TryAdd(validationResult.Key, validationResult.Value);
        });

        await _dataPointValidator.CheckDevicesRegistrationAsync(validationResults);

        return validationResults.ToDictionary();
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

    [Function(nameof(StoreHistoricalData))]
    public async Task StoreHistoricalData([ActivityTrigger] IEnumerable<DataPointEntity> dataPointEntities)
    {
        try
        {
            await _tableStorageClient.AddEntitiesBatchedAsync(dataPointEntities);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Exception while adding batch to table storage: {TableStorageAddException} @ {TableStorageAddStackTrace}",
                ex.Message, ex.StackTrace);
        }
    }

    [Function(nameof(StoreLastKnownValues))]
    public async Task StoreLastKnownValues([ActivityTrigger] IEnumerable<LastKnownValue> lastKnownValues)
    {
        try
        {
            var valuesDictionary = new Dictionary<string, List<LastKnownValue>>();

            foreach (var lastKnownValue in lastKnownValues)
            {
                if (!valuesDictionary.ContainsKey(lastKnownValue.Id))
                {
                    valuesDictionary[lastKnownValue.Id] = new List<LastKnownValue>();
                }
                valuesDictionary[lastKnownValue.Id].Add(lastKnownValue);
            }

            await _redisClient.StoreElementsAsync(valuesDictionary);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Exception while adding batch to cache: {CacheAddException} @ {CacheAddStackTrace}",
                ex.Message, ex.StackTrace);
        }
        
    }

    private IEnumerable<LastKnownValue> MapDataPointsToKnownValues(IEnumerable<DataPoint> dataPoints)
    {
        return dataPoints.Select(dp => new LastKnownValue
        {
            Id = dp.Id.ToString(),
            Timestamp = _timeReference.AddSeconds(dp.TimeOffsetSeconds),
            Longitude = _ingestionConfiguration.ReferenceLongitude + dp.X,
            Latitude = _ingestionConfiguration.ReferenceLatitude + dp.Y,
            Heading = dp.Heading,
            SpeedKmph = dp.Speed,
            AccelerationKmph = dp.Acceleration
        });
    }
}