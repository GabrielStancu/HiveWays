using Azure.Data.Tables;

namespace HiveWays.Business.TableStorageClient;

public interface ITableStorageClient<in T> where T : ITableEntity
{
    Task UpsertEntityAsync(T entity);
    Task AddEntitiesBatchedAsync(IEnumerable<T> entities);
    Task RemoveOldEntitiesAsync();
}
