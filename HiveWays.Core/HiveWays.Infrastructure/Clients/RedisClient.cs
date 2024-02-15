using System.Text.Json;
using HiveWays.Business.RedisClient;
using StackExchange.Redis;

namespace HiveWays.Infrastructure.Clients;

public class RedisClient<T> : IRedisClient<T>
{
    private readonly RedisConfiguration _redisConfiguration;
    private ConnectionMultiplexer _redisConnection;

    public RedisClient(RedisConfiguration redisConfiguration)
    {
        _redisConfiguration = redisConfiguration;
    }

    public async Task StoreElementsAsync(IDictionary<string, List<T>> elementLists)
    {
        InitRedisClient();
        var database = _redisConnection.GetDatabase();

        foreach (var elementList in elementLists)
        {
            database.ListTrim(elementList.Key, 0, -2);

            foreach (var element in elementList.Value)
            {
                await database.ListRightPushAsync(elementList.Key, JsonSerializer.Serialize(element));
            }
        }
    }

    public async Task<IEnumerable<T>> GetListElementsAsync(string key)
    {
        var database = _redisConnection.GetDatabase();
        var storedValues = await database.ListRangeAsync(key);
        var elements = storedValues
            .Select(e => JsonSerializer.Deserialize<T>(e));

        return elements;
    }

    private void InitRedisClient()
    {
        if (_redisConnection != null)
            return;

        _redisConnection = ConnectionMultiplexer.Connect(_redisConfiguration.ConnectionString);
    }
}
