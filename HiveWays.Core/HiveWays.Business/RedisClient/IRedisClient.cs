using HiveWays.Domain.Models;

namespace HiveWays.Business.RedisClient;

public interface IRedisClient<T> where T : IIdentifiable
{
    Task StoreElementsAsync(IEnumerable<T> elements);
    Task<IEnumerable<T>> GetElementsAsync();
}