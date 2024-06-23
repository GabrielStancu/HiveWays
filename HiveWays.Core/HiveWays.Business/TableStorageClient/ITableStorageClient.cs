using Azure.Data.Tables;

namespace HiveWays.Business.TableStorageClient;

public interface ITableStorageClient<T> where T : ITableEntity
{
    public Task<T> GetEntityAsync(string partitionKey, string rowKey);
    public Task UpsertEntityAsync(T entity);
    public Task AddEntitiesBatchedAsync(IEnumerable<T> entities);
    public Task RemoveOldEntitiesAsync();
}
