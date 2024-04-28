using Azure.Data.Tables;
using HiveWays.Business.Extensions;
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

    public async Task<T> GetEntityAsync(string partitionKey, string rowKey)
    {
        await InitTableClientAsync();

        try
        {
            return await _tableClient.GetEntityAsync<T>(partitionKey, rowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("Could not find entity with partition key {NotFoundPartitionKey} and row key {NotFoundRowKey}", partitionKey, rowKey);
            _logger.LogError("Exception while querying table storage: {TableStorageException} @ {TableStorageExceptionStackTrace}", ex.Message, ex.StackTrace);

            return null;
        }
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

    public async Task RemoveOldEntitiesAsync()
    {
        await InitTableClientAsync();

        var entities = await GetOldTableEntitiesAsync();

        foreach (var entityGroup in entities.GroupBy(e => e.PartitionKey))
        {
            foreach (var entitiesBatch in entityGroup.Batch(_configuration.BatchSize))
            {
                await DeleteEntitiesBatchASync(entitiesBatch);
            }
        }
    }

    private async Task<List<T>> GetOldTableEntitiesAsync()
    {
        var entities = new List<T>();
        var timestamp = DateTime.UtcNow.AddSeconds(-1 * _configuration.Ttl);
        var oldEntities = _tableClient.QueryAsync<T>(e => e.Timestamp < timestamp);

        await foreach (var oldEntity in oldEntities)
        {
            entities.Add(oldEntity);
        }

        return entities;
    }

    private async Task DeleteEntitiesBatchASync(IEnumerable<T> entitiesBatch)
    {
        var transactionBatch = new List<TableTransactionAction>();

        foreach (var entity in entitiesBatch.Where(e => e != null))
        {
            transactionBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
        }

        try
        {
            await _tableClient.SubmitTransactionAsync(transactionBatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Error while deleting from table storage. Exception: {TableStorageException} @ {TableStorageExceptionStackTrace}",
                ex.Message, ex.StackTrace);
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
