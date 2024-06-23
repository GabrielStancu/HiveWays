using HiveWays.Business.TableStorageClient;
using HiveWays.Domain.Entities;

namespace HiveWays.FleetIntegration.Business.Interfaces;

public interface IDeviceInfoTableClient : ITableStorageClient<DataPointEntity>
{
    new Task<DataPointEntity> GetEntityAsync(string partitionKey, string rowKey);
    new Task UpsertEntityAsync(DataPointEntity entity);
    new Task AddEntitiesBatchedAsync(IEnumerable<DataPointEntity> entities);
    new Task RemoveOldEntitiesAsync();
}