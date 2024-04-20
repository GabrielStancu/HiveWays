using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Entities;
using HiveWays.FleetIntegration.Business.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration;

public class StorageCleaner
{
    private readonly ITableStorageClient<DataPointEntity> _tableStorageClient;
    private readonly CleanupConfiguration _cleanupConfiguration;
    private readonly ILogger _logger;

    public StorageCleaner(ITableStorageClient<DataPointEntity> tableStorageClient,
        CleanupConfiguration cleanupConfiguration,
        ILogger<StorageCleaner> logger)
    {
        _tableStorageClient = tableStorageClient;
        _cleanupConfiguration = cleanupConfiguration;
        _logger = logger;
    }

    [Function("StorageCleaner")]
    public async Task Run([TimerTrigger("* * * * * *")] TimerInfo myTimer)
    {
        if (_cleanupConfiguration.DisabledEntitiesCleanup)
        {
            _logger.LogInformation("Entities cleanup is disabled, returning...");
            return;
        }

        try
        {
            _logger.LogInformation("Cleaning up old entities...");
            await _tableStorageClient.RemoveOldEntitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while deleting old entities. {DeleteException} @ {DeleteExceptionStackTrace}", ex.Message, ex.StackTrace);
        }
    }
}