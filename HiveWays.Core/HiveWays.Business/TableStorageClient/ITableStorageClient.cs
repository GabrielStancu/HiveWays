using Azure.Data.Tables;

namespace HiveWays.Business.TableStorageClient;

public interface ITableStorageClient<T> where T : ITableEntity
{
    Task<T> GetEntityAsync(string partitionKey, string rowKey);
    Task UpsertEntityAsync(T entity);
    Task AddEntitiesBatchedAsync(IEnumerable<T> entities);
    Task RemoveOldEntitiesAsync();
}
