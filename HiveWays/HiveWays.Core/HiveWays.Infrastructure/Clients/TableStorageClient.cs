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

    public async Task UpsertEntitiesBatchedAsync(IEnumerable<T> entities)
    {
        await InitTableClientAsync();

        var batch = new List<TableTransactionAction>();

        foreach (var entity in entities)
        {
            batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertMerge, entity));
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
