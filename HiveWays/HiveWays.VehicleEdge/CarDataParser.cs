using HiveWays.Business.Extensions;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Infrastructure.Factories;
using HiveWays.VehicleEdge.CarDataCsvParser;
using HiveWays.VehicleEdge.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge;

public class CarDataParser
{
    private readonly ICarDataCsvParser _csvParser;
    private readonly DeviceInfoConfiguration _deviceInfoConfiguration;
    private readonly IServiceBusSenderClient _senderClient;
    private readonly ILogger<CarDataParser> _logger;

    public CarDataParser(ICarDataCsvParser csvParser,
        IServiceBusSenderFactory senderFactory,
        CarEventsServiceBusConfiguration serviceBusConfiguration,
        DeviceInfoConfiguration deviceInfoConfiguration,
        ILogger<CarDataParser> logger)
    {
        _csvParser = csvParser;
        _deviceInfoConfiguration = deviceInfoConfiguration;
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

            foreach (var dataPointsBatch in dataPoints.Batch(_deviceInfoConfiguration.BatchSize))
            {
                _logger.LogInformation("Sending data point to service bus");
                await _senderClient.SendMessageAsync(dataPointsBatch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing blob. Exception: {ex.Message}");
            throw;
        }
    }
}