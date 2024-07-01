using HiveWays.Business.CosmosDbClient;
using HiveWays.VehicleEdge.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge;

public class VehicleDataReader
{
    private readonly ICosmosDbClient<VehicleData> _cosmosDbClient;
    private readonly ILogger<VehicleDataReader> _logger;

    public VehicleDataReader(ICosmosDbClient<VehicleData> cosmosDbClient,
        ILogger<VehicleDataReader> logger)
    {
        _cosmosDbClient = cosmosDbClient;
        _logger = logger;
    }

    [Function("VehicleDataReader")]
    public async Task<IEnumerable<VehicleData>> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        try
        {
            var deltaTimestamp = DateTime.UtcNow.AddSeconds(-5);
            var deltaData = (await _cosmosDbClient.GetDocumentsByQueryAsync(devices =>
            {
                var filteredDevices = devices.Where(d => d.Timestamp > deltaTimestamp);
                return filteredDevices as IOrderedQueryable<VehicleData>;
            })).ToList();
            return deltaData;
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception while fetching vehicle data: {FetchDataEx} @ {FetchDataExStackTrace}", ex.Message, ex.StackTrace);
            return new List<VehicleData>();
        }
    }
}