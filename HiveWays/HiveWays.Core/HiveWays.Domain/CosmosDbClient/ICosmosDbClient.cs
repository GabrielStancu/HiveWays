using HiveWays.Domain.Items;

namespace HiveWays.Business.CosmosDbClient;

public interface ICosmosDbClient<T> where T : BaseItem
{
    Task<T> GetItemByIdAsync(string id, string partitionKey);
    Task<IEnumerable<T>> GetItemsAsync();
    Task<IEnumerable<T>> GetItemsByQueryAsync(Func<T, bool> query);
    Task UpsertItemAsync(T entity);
}