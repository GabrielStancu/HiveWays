using Azure;
using Azure.Data.Tables;
using HiveWays.VehicleEdge.Configuration;
using HiveWays.VehicleEdge.Models;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge.Business;
public class CarDataTableClient : ICarDataTableClient
{
    private readonly TableClientConfiguration _configuration;
    private TableClient _tableClient;
    private readonly ILogger _logger;
    private const string TableName = "CarsStatus";

    public CarDataTableClient(TableClientConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<CarDataTableClient>();
    }

    public async Task WriteCarDataAsync(IEnumerable<DataPointEntity> entities)
    {
        await InitTableClientAsync();

        var upsertTasks = new List<Task>();

        foreach (var entity in entities)
        {
            var upsertTask = WriteCarDataAsync(entity);
            upsertTasks.Add(upsertTask);
        }

        await Task.WhenAll(upsertTasks);
    }

    private async Task<Response> WriteCarDataAsync(DataPointEntity entity)
    {
        var response = await _tableClient.UpsertEntityAsync(entity);
        _logger.LogInformation("Upserted alert entity with PartitionKey {PartitionKey}, RowKey {RowKey}", entity.PartitionKey, entity.RowKey);

        return response;
    }

    private async Task InitTableClientAsync()
    {
        _logger.LogInformation("Initializing table client for {TableName}", TableName);

        if (_tableClient != null)
        {
            _logger.LogInformation("Table client already initialized, skipping...");
            return;
        }

        var serviceClient = new TableServiceClient(_configuration.ConnectionString);
        _tableClient = serviceClient.GetTableClient(TableName);
        await _tableClient.CreateIfNotExistsAsync();

        _logger.LogInformation("Created table client for {TableName}", TableName);
    }
}
