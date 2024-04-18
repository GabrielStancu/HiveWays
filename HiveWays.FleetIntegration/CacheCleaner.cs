using HiveWays.Business.RedisClient;
using HiveWays.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration;

public class CacheCleaner
{
    private readonly IRedisClient<VehicleStats> _redisClient;
    private readonly RedisConfiguration _redisConfiguration;
    private readonly ILogger _logger;

    public CacheCleaner(IRedisClient<VehicleStats> redisClient,
        RedisConfiguration redisConfiguration,
        ILogger<CacheCleaner> logger)
    {
        _redisClient = redisClient;
        _redisConfiguration = redisConfiguration;
        _logger = logger;
    }

    [Function("CacheCleaner")]
    public async Task Run([TimerTrigger("* */1 * * * *")] TimerInfo myTimer)
    {
        try
        {
            var vehicleStats = await _redisClient.GetElementsAsync();

            foreach (var vehicleInfo in vehicleStats.GroupBy(v => v.Id))
            {
                var lastTimestamp = vehicleInfo.Select(v => v.Timestamp).Max();
                if (DateTime.UtcNow - lastTimestamp > TimeSpan.FromSeconds(_redisConfiguration.Ttl))
                {
                    _logger.LogInformation("Deleting values for id {DeletedId} due to inactivity", vehicleInfo.Key);
                    await _redisClient.DeleteKeyAsync(vehicleInfo.Key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while deleting old keys. {DeleteException} @ {DeleteExceptionStackTrace}", ex.Message, ex.StackTrace);
        }
    }
}