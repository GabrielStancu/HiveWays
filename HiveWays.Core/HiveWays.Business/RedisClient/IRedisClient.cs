namespace HiveWays.Business.RedisClient;

public interface IRedisClient<T>
{
    Task StoreElementsAsync(IEnumerable<T> elements);
    Task<IEnumerable<T>> GetListElementsAsync(string key);
}