using Azure.Data.Tables;
using HiveWays.Business.TableStorageClient;

namespace HiveWays.Infrastructure.Clients;

public class TableStorageClient<T> : ITableStorageClient<T> where T : class, ITableEntity
{
    private readonly TableStorageConfiguration _configuration;
    private TableClient _tableClient;

    public TableStorageClient(TableStorageConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task UpsertEntityAsync(T entity)
    {
        await InitTableClientAsync();

        await _tableClient.UpsertEntityAsync(entity);
    }

    // Notes:
    // 1. This works across multiple partition keys only with Azurite (locally). In Azure this has to be batched by partition key
    // 2. Max size: 100 items/4MB
    // 3. If moved to Azure storage, we should change the partition key, maybe to city instead of car id?
    // Source: https://medium.com/medialesson/azure-data-tables-baabaea75656
    public async Task AddEntitiesBatchedAsync(IEnumerable<T> entities)
    {
        await InitTableClientAsync();

        var batch = new List<TableTransactionAction>();

        foreach (var entity in entities)
        {
            batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
        }

        await _tableClient.SubmitTransactionAsync(batch);
    }


    private async Task InitTableClientAsync()
    {
        if (_tableClient != null)
            return;

        var serviceClient = new TableServiceClient(_configuration.ConnectionString);
        _tableClient = serviceClient.GetTableClient(_configuration.TableName);
        await _tableClient.CreateIfNotExistsAsync();
    }
}
