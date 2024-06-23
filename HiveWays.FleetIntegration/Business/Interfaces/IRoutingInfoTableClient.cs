using HiveWays.Business.TableStorageClient;
using HiveWays.FleetIntegration.Models;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface IRoutingInfoTableClient : ITableStorageClient<RoutingInfoEntity>
{
    new Task<RoutingInfoEntity> GetEntityAsync(string partitionKey, string rowKey);
    new Task UpsertEntityAsync(RoutingInfoEntity entity);
    new Task AddEntitiesBatchedAsync(IEnumerable<RoutingInfoEntity> entities);
    new Task RemoveOldEntitiesAsync();
}