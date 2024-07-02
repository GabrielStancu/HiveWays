using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.Extensions;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Domain.Models;
using HiveWays.Infrastructure.Factories;
using HiveWays.VehicleEdge.CarDataCsvParser;
using HiveWays.VehicleEdge.Configuration;
using HiveWays.VehicleEdge.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HiveWays.VehicleEdge;

public class CarDataParser
{
    private readonly ICarDataCsvParser<DataPoint> _csvParser;
    private readonly DeviceInfoConfiguration _deviceInfoConfiguration;
    private readonly ICosmosDbClient<VehicleData> _cosmosDbClient;
    private readonly IServiceBusSenderClient _senderClient;
    private readonly ILogger<CarDataParser> _logger;

    public CarDataParser(ICarDataCsvParser<DataPoint> csvParser,
        IServiceBusSenderFactory senderFactory,
        CarEventsServiceBusConfiguration serviceBusConfiguration,
        DeviceInfoConfiguration deviceInfoConfiguration,
        ICosmosDbClient<VehicleData> cosmosDbClient,
        ILogger<CarDataParser> logger)
    {
        _csvParser = csvParser;
        _deviceInfoConfiguration = deviceInfoConfiguration;
        _cosmosDbClient = cosmosDbClient;
        _senderClient = senderFactory.GetServiceBusSenderClient(serviceBusConfiguration.ConnectionString, serviceBusConfiguration.EventReceivedQueueName);
        _logger = logger;
    }

    [Function(nameof(CarDataParser))]
    public async Task Run([BlobTrigger("cars/{file}", Connection = "StorageAccount:ConnectionString")] Stream stream, string file)
    {
        try
        {
            _logger.LogInformation("Parsing file {File}", file);
            var dataPoints = _csvParser.ParseCsv(stream);
            var fileParsing = ParseBatchDescriptor(file);

            foreach (var dataPointsBatched in dataPoints.Batch(_deviceInfoConfiguration.BatchSize))
            {
                var batchedList = dataPointsBatched.ToList();

                await ExportServiceBusBatchAsync(file, batchedList);
                await ExportCosmosDbBatchAsync(batchedList, fileParsing.RoadId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing blob. Exception: {ex.Message}");
            throw;
        }
    }

    private async Task ExportServiceBusBatchAsync(string file, List<DataPoint> dataPointsBatched)
    {
        _logger.LogInformation("Sending data points batch to service bus");

        var dataPointsBatch = new DataPointsBatch
        {
            BatchDescriptor = file,
            DataPoints = dataPointsBatched
        };

        await _senderClient.SendMessageAsync(dataPointsBatch);
    }

    private async Task ExportCosmosDbBatchAsync(List<DataPoint> batchedList, int roadId)
    {
        _logger.LogInformation("Sending data points batch to cosmos db");

        var vehiclesData = batchedList.Select(b => new VehicleData
        {
            Id = $"{b.Id}-{Guid.NewGuid()}",
            Timestamp = DateTime.UtcNow,
            DataPoint = b,
            RoadId = roadId
        }).ToList();

        await _cosmosDbClient.BulkUpsertAsync(vehiclesData);
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

        return (roadId, referenceTimestamp.ToUniversalTime());
    }
}