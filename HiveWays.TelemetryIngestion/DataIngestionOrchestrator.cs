using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using HiveWays.Business.RedisClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Entities;
using HiveWays.Domain.Models;
using HiveWays.TelemetryIngestion.Business;
using HiveWays.TelemetryIngestion.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace HiveWays.TelemetryIngestion;

public class DataIngestionOrchestrator
{
    private readonly IDataPointValidator _dataPointValidator;
    private readonly ITableStorageClient<DataPointEntity> _tableStorageClient;
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly ILogger<DataIngestionOrchestrator> _logger;

    public DataIngestionOrchestrator(
        IDataPointValidator dataPointValidator,
        ITableStorageClient<DataPointEntity> tableStorageClient,
        IRedisClient<VehicleStats> redisClient,
        ILogger<DataIngestionOrchestrator> logger)
    {
        _dataPointValidator = dataPointValidator;
        _tableStorageClient = tableStorageClient;
        _redisClient = redisClient;
        _logger = logger;
    }

    [Function(nameof(DataIngestionOrchestrator))]
    public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var dataPointsBatch = context.GetInput<DataPointsBatch>();
        var validDataPointsBatch = await ValidateDataPointsBatchAsync(context, dataPointsBatch);

        var vehicleStats = MapDataPointsToVehicleStats(validDataPointsBatch);
        await context.CallActivityAsync(nameof(StoreVehicleStats), vehicleStats);

        var validDataPointEntities = await context.CallActivityAsync<IEnumerable<DataPointEntity>>(nameof(EnrichDataPoints), validDataPointsBatch);
        await context.CallActivityAsync(nameof(StoreHistoricalData), validDataPointEntities);
    }

    private async Task<DataPointsBatch> ValidateDataPointsBatchAsync(TaskOrchestrationContext context, DataPointsBatch dataPointsBatch)
    {
        var validationResults = await context.CallActivityAsync<IEnumerable<ValidatedDataPoint>>(nameof(ValidateDataPoints), dataPointsBatch.DataPoints);
        var validDataPoints = validationResults
            .Where(vr => vr.IsValid)
            .Select(vr => vr.DataPoint)
            .ToList();

        return new DataPointsBatch
        {
            DataPoints = validDataPoints,
            BatchDescriptor = dataPointsBatch.BatchDescriptor
        };
    }

    [Function(nameof(ValidateDataPoints))]
    public async Task<List<ValidatedDataPoint>> ValidateDataPoints([ActivityTrigger] IEnumerable<DataPoint> dataPoints)
    {
        var validationResults = new ConcurrentBag<ValidatedDataPoint>();

        Parallel.ForEach(dataPoints, dataPoint =>
        {
            var validationResult = _dataPointValidator.ValidateDataPointRange(dataPoint);
            validationResults.Add(validationResult);
        });

        await _dataPointValidator.CheckDevicesRegistrationAsync(validationResults);

        return validationResults.ToList();
    }

    [Function(nameof(EnrichDataPoints))]
    public IEnumerable<DataPointEntity> EnrichDataPoints([ActivityTrigger] DataPointsBatch dataPointsBatch)
    {
        var entities = new List<DataPointEntity>();
        var (roadId, referenceTimestamp) = ParseBatchDescriptor(dataPointsBatch.BatchDescriptor);

        Parallel.ForEach(dataPointsBatch.DataPoints, dataPoint =>
        {
            var dataPointEntity = new DataPointEntity
            {
                PartitionKey = referenceTimestamp.AddSeconds(dataPoint.TimeOffsetSeconds).ToString("o"),
                RowKey = dataPoint.Id.ToString(),
                RoadId = roadId,
                SpeedMps = dataPoint.Speed,
                SpeedKmph = dataPoint.Speed * 3.6,
                AccelerationMps = dataPoint.Acceleration,
                AccelerationKmph = dataPoint.Acceleration * 12960,
                Latitude = dataPoint.Y,
                Longitude = dataPoint.X,
                Heading = dataPoint.Heading
            };
            entities.Add(dataPointEntity);
        });

        return entities;
    }

    [Function(nameof(StoreHistoricalData))]
    public async Task StoreHistoricalData([ActivityTrigger] IEnumerable<DataPointEntity> dataPointEntities)
    {
        foreach (var entitiesPartitionGroup in dataPointEntities.GroupBy(e => e.PartitionKey))
        {
            try
            {
                await _tableStorageClient.AddEntitiesBatchedAsync(entitiesPartitionGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception while adding batch to table storage: {TableStorageAddException} @ {TableStorageAddStackTrace}",
                    ex.Message, ex.StackTrace);
            }
        }
    }

    [Function(nameof(StoreVehicleStats))]
    public async Task StoreVehicleStats([ActivityTrigger] IEnumerable<VehicleStats> vehicleStats)
    {
        try
        {
            await _redisClient.StoreElementsAsync(vehicleStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Exception while adding batch to cache: {CacheAddException} @ {CacheAddStackTrace}",
                ex.Message, ex.StackTrace);
        }
    }

    private IEnumerable<VehicleStats> MapDataPointsToVehicleStats(DataPointsBatch dataPointsBatch)
    {
        var (roadId, referenceTimestamp) = ParseBatchDescriptor(dataPointsBatch.BatchDescriptor);

        return dataPointsBatch.DataPoints.Select(dp => new VehicleStats
        {
            Id = dp.Id,
            Timestamp = referenceTimestamp.AddSeconds(dp.TimeOffsetSeconds),
            RoadId = roadId,
            Longitude = dp.X,
            Latitude = dp.Y,
            Heading = dp.Heading,
            SpeedKmph = dp.Speed,
            AccelerationKmph = dp.Acceleration
        });
    }

    private (int RoadId, DateTime ReferenceTimestamp) ParseBatchDescriptor(string batchDescriptor)
    {
        var pattern = "road(.*)_time(.*).csv";
        var match = Regex.Match(batchDescriptor, pattern);

        if (!match.Success || match.Groups.Count < 3)
        {
            _logger.LogError("Could not parse batch descriptor {NotParsedBatchDescriptor}", batchDescriptor);
            return (-1, DateTime.MinValue);
        }

        var hasParsedRoadId = int.TryParse(match.Groups[1].ToString(), out var roadId);
        var hasParsedTimestamp = DateTime.TryParse(match.Groups[2].ToString().Replace('-', '/').Replace('_', ':'), out var referenceTimestamp);

        if (!hasParsedRoadId || !hasParsedTimestamp)
        {
            _logger.LogError("Could not parse road id {NotParsedRoadId} or reference time {NotParsedReferenceTime}", match.Groups[1].ToString(), match.Groups[2].ToString());
            return (-1, DateTime.MinValue);
        }

        return (roadId, referenceTimestamp);
    }
}