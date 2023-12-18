using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using HiveWays.VehicleEdge.Business;
using HiveWays.VehicleEdge.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using HiveWays.VehicleEdge.Extensions;

namespace HiveWays.VehicleEdge
{
    public class CarDataParser
    {
        private readonly ICarDataTableClient _carDataTableClient;
        private readonly ILogger<CarDataParser> _logger;

        public CarDataParser(ICarDataTableClient carDataTableClient, ILogger<CarDataParser> logger)
        {
            _carDataTableClient = carDataTableClient;
            _logger = logger;
        }

        [Function(nameof(CarDataParser))]
        public async Task Run([BlobTrigger("cars/{file}", Connection = "StorageAccount:ConnectionString")] Stream stream, string file)
        {
            try
            {
                _logger.LogInformation("Parsing file {File}", file);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    NewLine = Environment.NewLine,
                };
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, config);

                var timeReference = DateTime.UtcNow;
                var dataPoints = csv.GetRecords<DataPoint>();
                var dataPointsEntities = dataPoints
                    .Select(dp => new DataPointEntity(dp, timeReference));

                foreach (var batch in dataPointsEntities.Batch(50))
                {
                    _logger.LogInformation("Upserting batch to the table storage");
                    await _carDataTableClient.WriteCarDataAsync(batch);
                    _logger.LogInformation("Successfully uploaded batch to the table storage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing blob. Exception: {ex.Message}");
                throw;
            }
        }
    }
}
