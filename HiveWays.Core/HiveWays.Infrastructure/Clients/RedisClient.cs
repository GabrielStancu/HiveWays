using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using StackExchange.Redis;

namespace HiveWays.Infrastructure.Clients;

public class RedisClient<T> : IRedisClient<T> where T : IIdentifiable
{
    private readonly RedisConfiguration _redisConfiguration;
    private IDatabase _database;

    public RedisClient(RedisConfiguration redisConfiguration)
    {
        _redisConfiguration = redisConfiguration;
    }

    public async Task StoreElementsAsync(IEnumerable<T> elements)
    {
        InitDatabase();
        
        var batch = _database.CreateBatch();
        var key = new RedisKey("cars-last-known-values");

        foreach (var element in elements)
        {
            var value = JsonSerializer.Serialize(element);
            await batch.SortedSetAddAsync(key, value, element.Id);
        }

        await batch.KeyExpireAsync(key, TimeSpan.FromSeconds(_redisConfiguration.ExpirationTime));
        batch.Execute();
    }

    public async Task<IEnumerable<T>> GetElementsAsync()
    {
        InitDatabase();

        var elements = await _database.SortedSetRangeByScoreAsync("cars-last-known-values");

        return elements as IEnumerable<T>;
    }

    private void InitDatabase()
    {
        if (_database != null)
            return;

        var redisConnection = ConnectionMultiplexer.Connect(_redisConfiguration.ConnectionString);
        _database = redisConnection.GetDatabase();
    }
}
