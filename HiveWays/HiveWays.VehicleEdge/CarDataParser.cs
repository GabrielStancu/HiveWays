using HiveWays.Business.CarDataCsvParser;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge;

public class CarDataParser
{
    private readonly ICarDataCsvParser _csvParser;
    private readonly IQueueSenderClient<DataPoint> _queueSender;
    private readonly ILogger<CarDataParser> _logger;

    public CarDataParser(ICarDataCsvParser csvParser,
        IQueueSenderClient<DataPoint> queueSender,
        ILogger<CarDataParser> logger)
    {
        _csvParser = csvParser;
        _queueSender = queueSender;
        _logger = logger;
    }

    [Function(nameof(CarDataParser))]
    public async Task Run([BlobTrigger("cars/{file}", Connection = "StorageAccount:ConnectionString")] Stream stream, string file)
    {
        try
        {
            _logger.LogInformation("Parsing file {File}", file);
            var dataPoints = _csvParser.ParseCsv(stream);

            foreach (var dataPoint in dataPoints)
            {
                _logger.LogInformation("Sending data point to service bus");
                await _queueSender.SendMessageAsync(dataPoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing blob. Exception: {ex.Message}");
            throw;
        }
    }
}