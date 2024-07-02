using HiveWays.Business.CosmosDbClient;
using HiveWays.VehicleEdge.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge;

public class CosmosCleaner
{
    private readonly ICosmosDbClient<VehicleData> _cosmosDbClient;
    private readonly ILogger _logger;

    public CosmosCleaner(ICosmosDbClient<VehicleData> cosmosDbClient,
        ILogger<CosmosCleaner> logger)
    {
        _cosmosDbClient = cosmosDbClient;
        _logger = logger;
    }

    [Function("CosmosCleaner")]
    public async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
    {
        try
        {
            var removeTimestamp = DateTime.UtcNow.AddSeconds(-10);
            var oldData = (await _cosmosDbClient.GetDocumentsByQueryAsync(devices =>
            {
                var filteredDevices = devices.Where(d => d.Timestamp < removeTimestamp);
                return filteredDevices as IOrderedQueryable<VehicleData>;
            })).ToList();

            await _cosmosDbClient.BulkDeleteAsync(oldData);
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception while deleting old vehicle data: {DeleteDataEx} @ {DeleteDataExStackTrace}", ex.Message, ex.StackTrace);
        }
    }
}