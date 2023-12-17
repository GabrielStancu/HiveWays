using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using HiveWays.VehicleEdge.Business;
using HiveWays.VehicleEdge.Models;

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
        public async Task Run([BlobTrigger("cars/{car}/data/{file}", Connection = "CarDataConnectionString:blob")] Stream stream,
            string car, string file)
        {
            try
            {
                _logger.LogInformation("Parsing file {File} for car {Car}", file, car);

                using var reader = new StreamReader(stream);
                string jsonContent = await reader.ReadToEndAsync();
                var data = JsonSerializer.Deserialize<List<Batch>>(jsonContent);
                var dataEntities = data
                    .OrderBy(b => b.Values.First().Dp.First().Ts)
                    .Select(b => new ValueEntity(car, b))
                    .ToList();

                _logger.LogInformation("Upserting {DataNodesCount} nodes to the table storage", dataEntities.Count);
                await _carDataTableClient.WriteCarDataAsync(dataEntities);
                _logger.LogInformation("Successfully uploaded data nodes to the table storage");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing blob. Exception: {ex.Message}");
                throw;
            }
        }
    }
}
