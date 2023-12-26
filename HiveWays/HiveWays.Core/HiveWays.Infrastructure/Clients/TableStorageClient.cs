using Azure;
using Azure.Data.Tables;
using HiveWays.Business.TableStorageClient;
using Microsoft.Extensions.Logging;

namespace HiveWays.Infrastructure.Clients;

public class TableStorageClient<T> : ITableStorageClient<T> where T : class, ITableEntity
{
    private readonly ClientConfiguration _configuration;
    private TableClient _tableClient;
    private readonly ILogger _logger;

    public TableStorageClient(ClientConfiguration configuration, ILogger<TableStorageClient<T>> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task UpsertEntitiesAsync(IEnumerable<T> entities)
    {
        await InitTableClientAsync();

        var upsertTasks = new List<Task>();

        foreach (var entity in entities)
        {
            var upsertTask = UpsertEntityAsync(entity);
            upsertTasks.Add(upsertTask);
        }

        await Task.WhenAll(upsertTasks);
    }

    private async Task<Response> UpsertEntityAsync(T entity)
    {
        var response = await _tableClient.UpsertEntityAsync(entity);
        _logger.LogInformation("Upserted alert entity with PartitionKey {PartitionKey}, RowKey {RowKey}", entity.PartitionKey, entity.RowKey);

        return response;
    }

    private async Task InitTableClientAsync()
    {
        if (_tableClient != null)
            return;

        var serviceClient = new TableServiceClient(_configuration.ConnectionString);
        _tableClient = serviceClient.GetTableClient(_configuration.TableName);
        await _tableClient.CreateIfNotExistsAsync();

        _logger.LogInformation("Created table client for {TableName}", _configuration.TableName);
    }
}
