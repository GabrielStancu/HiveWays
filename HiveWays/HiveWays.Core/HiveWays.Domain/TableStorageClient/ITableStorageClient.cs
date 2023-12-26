namespace HiveWays.Business.TableStorageClient;

public interface ITableStorageClient<in T> where T : class
{
    Task UpsertEntitiesAsync(IEnumerable<T> entities);
}
