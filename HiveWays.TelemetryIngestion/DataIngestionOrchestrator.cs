using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using HiveWays.Business.RedisClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Entities;
using HiveWays.Domain.Models;
using HiveWays.TelemetryIngestion.Business;
using HiveWays.TelemetryIngestion.Configuration;
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
    private readonly IngestionConfiguration _ingestionConfiguration;
    private readonly ILogger<DataIngestionOrchestrator> _logger;

    private int _roadId;
    private DateTime _referenceTimestamp;

    public DataIngestionOrchestrator(
        IDataPointValidator dataPointValidator,
        ITableStorageClient<DataPointEntity> tableStorageClient,
        IRedisClient<VehicleStats> redisClient,
        IngestionConfiguration ingestionConfiguration,
        ILogger<DataIngestionOrchestrator> logger)
    {
        _dataPointValidator = dataPointValidator;
        _tableStorageClient = tableStorageClient;
        _redisClient = redisClient;
        _ingestionConfiguration = ingestionConfiguration;
        _logger = logger;
    }

    [Function(nameof(DataIngestionOrchestrator))]
    public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var dataPointsBatch = context.GetInput<DataPointsBatch>();
        var validDataPoints = await ValidateDataPointsBatchAsync(context, dataPointsBatch);

        var vehicleStats = MapDataPointsToVehicleStats(validDataPoints);
        await context.CallActivityAsync(nameof(StoreVehicleStats), vehicleStats);

        var validDataPointEntities = await context.CallActivityAsync<IEnumerable<DataPointEntity>>(nameof(EnrichDataPoints), validDataPoints);
        await context.CallActivityAsync(nameof(StoreHistoricalData), validDataPointEntities);
    }

    private async Task<List<DataPoint>> ValidateDataPointsBatchAsync(TaskOrchestrationContext context, DataPointsBatch dataPointsBatch)
    {
        var validationResults = await context.CallActivityAsync<IEnumerable<ValidatedDataPoint>>(nameof(ValidateDataPoints), dataPointsBatch.DataPoints);
        var validDataPoints = validationResults
            .Where(vr => vr.IsValid)
            .Select(vr => vr.DataPoint)
            .ToList();

        ParseBatchDescriptor(dataPointsBatch.BatchDescriptor);

        return validDataPoints;
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

        Parallel.ForEach(dataPointsBatch.DataPoints, dataPoint =>
        {
            var dataPointEntity = new DataPointEntity
            {
                PartitionKey = dataPoint.Id.ToString(),
                RowKey = _referenceTimestamp.AddSeconds(dataPoint.TimeOffsetSeconds).ToString("o"),
                RoadId = _roadId,
                SpeedMps = dataPoint.Speed,
                SpeedKmph = dataPoint.Speed * 3.6,
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

    private IEnumerable<VehicleStats> MapDataPointsToVehicleStats(IEnumerable<DataPoint> dataPoints)
    {
        return dataPoints.Select(dp => new VehicleStats
        {
            Id = dp.Id,
            Timestamp = _referenceTimestamp.AddSeconds(dp.TimeOffsetSeconds),
            RoadId = _roadId,
            Longitude = _ingestionConfiguration.ReferenceLongitude + dp.X,
            Latitude = _ingestionConfiguration.ReferenceLatitude + dp.Y,
            Heading = dp.Heading,
            SpeedKmph = dp.Speed,
            AccelerationKmph = dp.Acceleration
        });
    }

    private void ParseBatchDescriptor(string batchDescriptor)
    {
        var pattern = "road(.*)_time(.*).csv";
        var match = Regex.Match(batchDescriptor, pattern);

        if (!match.Success || match.Groups.Count < 3)
        {
            _logger.LogError("Could not parse batch descriptor {NotParsedBatchDescriptor}", batchDescriptor);
            return;
        }

        var hasParsedRoadId = int.TryParse(match.Groups[1].ToString(), out _roadId);
        var hasParsedTimestamp = DateTime.TryParse(match.Groups[2].ToString().Replace('-', '/').Replace('_', ':'), out _referenceTimestamp);

        if (!hasParsedRoadId || !hasParsedTimestamp)
        {
            _logger.LogError("Could not parse road id {NotParsedRoadId} or reference time {NotParsedReferenceTime}", match.Groups[1].ToString(), match.Groups[2].ToString());
        }
    }
}