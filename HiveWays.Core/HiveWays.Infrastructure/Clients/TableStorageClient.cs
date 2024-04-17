using Azure.Data.Tables;
using HiveWays.Business.TableStorageClient;
using Microsoft.Extensions.Logging;

namespace HiveWays.Infrastructure.Clients;

public class TableStorageClient<T> : ITableStorageClient<T> where T : class, ITableEntity
{
    private readonly TableStorageConfiguration _configuration;
    private readonly ILogger<TableStorageClient<T>> _logger;
    private TableClient _tableClient;

    public TableStorageClient(TableStorageConfiguration configuration, ILogger<TableStorageClient<T>> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task UpsertEntityAsync(T entity)
    {
        await InitTableClientAsync();

        await _tableClient.UpsertEntityAsync(entity);
    }

    public async Task AddEntitiesBatchedAsync(IEnumerable<T> entities)
    {
        await InitTableClientAsync();

        var batch = new List<TableTransactionAction>();

        foreach (var entity in entities.Where(e => e != null))
        {
            batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
        }

        try
        {
            await _tableClient.SubmitTransactionAsync(batch);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while saving to table storage. Exception: {TableStorageException} @ {TableStorageExceptionStackTrace}", ex.Message, ex.StackTrace);
        }
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
