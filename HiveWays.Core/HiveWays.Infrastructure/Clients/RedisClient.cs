using System.Collections.Concurrent;
using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using StackExchange.Redis;

namespace HiveWays.Infrastructure.Clients;

public class RedisClient<T> : IRedisClient<T> where T : IIdentifiable
{
    private readonly RedisConfiguration _redisConfiguration;
    private ConnectionMultiplexer _connectionMultiplexer;
    private IDatabase _database;

    public RedisClient(RedisConfiguration redisConfiguration)
    {
        _redisConfiguration = redisConfiguration;
    }

    public async Task StoreElementsAsync(IEnumerable<T> elements)
    {
        InitDatabase();

        foreach (var element in elements)
        {
            var redisKey = new RedisKey(element.Id.ToString());
            var value = JsonSerializer.Serialize(element);

            await _database.ListLeftPushAsync(redisKey, value);
            await _database.ListTrimAsync(redisKey, 0, _redisConfiguration.ListLength - 1);
        }
    }

    public async Task<IEnumerable<T>> GetElementsAsync()
    {
        InitDatabase();

        var elements = new ConcurrentBag<T>();
        var keys = _connectionMultiplexer
            .GetServer(_redisConfiguration.ConnectionString)
            .Keys();
        var tasks = keys.Select(async key =>
        {
            var redisList = await _database.ListRangeAsync(key);
            redisList.Select(e => JsonSerializer.Deserialize<T>(e))
                .ToList()
                .ForEach(e => elements.Add(e));
        });

        await Task.WhenAll(tasks);

        return elements;
    }

    public async Task DeleteKeyAsync(int id)
    {
        InitDatabase();

        var redisKey = new RedisKey(id.ToString());
        await _database.KeyDeleteAsync(redisKey);
    }

    private void InitDatabase()
    {
        if (_database != null)
            return;

        _connectionMultiplexer = ConnectionMultiplexer.Connect(_redisConfiguration.ConnectionString);
        _database = _connectionMultiplexer.GetDatabase();
    }
}
