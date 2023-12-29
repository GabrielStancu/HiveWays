namespace HiveWays.Business.TableStorageClient;

public interface ITableStorageClient<in T> where T : class
{
    Task UpsertEntityAsync(T entity);
    Task UpsertEntitiesAsync(IEnumerable<T> entities);
}
