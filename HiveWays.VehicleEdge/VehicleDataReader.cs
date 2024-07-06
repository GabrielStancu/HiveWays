using HiveWays.Business.CosmosDbClient;
using HiveWays.Business.TableStorageClient;
using HiveWays.VehicleEdge.Business;
using HiveWays.VehicleEdge.Configuration;
using HiveWays.VehicleEdge.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge;

public class VehicleDataReader
{
    private readonly ICosmosDbClient<VehicleData> _cosmosDbClient;
    private readonly ITrafficBalancerService _trafficBalancerService;
    private readonly ITableStorageClient<RoutingInfoEntity> _tableStorageClient;
    private readonly RoadConfiguration _roadConfiguration;
    private readonly ILogger<VehicleDataReader> _logger;

    public VehicleDataReader(ICosmosDbClient<VehicleData> cosmosDbClient,
        ITrafficBalancerService trafficBalancerService,
        ITableStorageClient<RoutingInfoEntity> tableStorageClient,
        RoadConfiguration roadConfiguration,
        ILogger<VehicleDataReader> logger)
    {
        _cosmosDbClient = cosmosDbClient;
        _trafficBalancerService = trafficBalancerService;
        _tableStorageClient = tableStorageClient;
        _roadConfiguration = roadConfiguration;
        _logger = logger;
    }

    [Function("VehicleDataReader")]
    public async Task<IEnumerable<VehicleData>> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        try
        {
            var deltaTimestamp = DateTime.UtcNow.AddSeconds(-10);
            var deltaData = (await _cosmosDbClient.GetDocumentsByQueryAsync(devices =>
            {
                var filteredDevices = devices.Where(d => d.Timestamp > deltaTimestamp);
                return filteredDevices as IOrderedQueryable<VehicleData>;
            })).ToList();

            var newRatio = _trafficBalancerService.RecomputeBalancingRatio(deltaData);
            _logger.LogInformation("New ratio computed: {NewRatio}", newRatio);

            await _tableStorageClient.UpsertEntityAsync(new RoutingInfoEntity
            {
                PartitionKey = _roadConfiguration.MainRoadId.ToString(),
                RowKey = _roadConfiguration.SecondaryRoadId.ToString(),
                Value = newRatio
            });

            return deltaData;
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception while fetching vehicle data: {FetchDataEx} @ {FetchDataExStackTrace}", ex.Message, ex.StackTrace);
            return new List<VehicleData>();
        }
    }
}