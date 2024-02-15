namespace HiveWays.Business.RedisClient;

public interface IRedisClient<T>
{
    Task StoreElementsAsync(IDictionary<string, List<T>> elementLists);
    Task<IEnumerable<T>> GetListElementsAsync(string key);
}