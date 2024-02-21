using System.Text.Json;
using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using StackExchange.Redis;

namespace HiveWays.Infrastructure.Clients;

public class RedisClient<T> : IRedisClient<T> where T : IIdentifiable
{
    private readonly RedisConfiguration _redisConfiguration;
    private ConnectionMultiplexer _redisConnection;

    public RedisClient(RedisConfiguration redisConfiguration)
    {
        _redisConfiguration = redisConfiguration;
    }

    public async Task StoreElementsAsync(IEnumerable<T> elements)
    {
        InitRedisClient();
        var database = _redisConnection.GetDatabase();
        var batch = database.CreateBatch();
        var batchTasks = new List<Task>();

        foreach (var element in elements)
        {
            var key = new RedisKey("cars-last-known-values");
            var value = JsonSerializer.Serialize(element);
            var addTask = batch.SortedSetAddAsync(key, value, element.Id).ContinueWith(_ =>
            {
                batch.KeyExpireAsync(key, TimeSpan.FromSeconds(5));
            });

            batchTasks.Add(addTask);
        }

        await Task.WhenAll(batchTasks);
        batch.Execute();
    }

    public async Task<IEnumerable<T>> GetElementsAsync()
    {
        InitRedisClient();
        var database = _redisConnection.GetDatabase();
        var elements = await database.SortedSetRangeByScoreAsync("cars-last-known-values");

        return elements as IEnumerable<T>;
    }

    private void InitRedisClient()
    {
        if (_redisConnection != null)
            return;

        _redisConnection = ConnectionMultiplexer.Connect(_redisConfiguration.ConnectionString);
    }
}
