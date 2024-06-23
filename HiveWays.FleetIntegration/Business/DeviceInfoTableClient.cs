using HiveWays.Domain.Entities;
using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business;
public class DeviceInfoTableClient : TableStorageClient<DataPointEntity>, IDeviceInfoTableClient
{
    public DeviceInfoTableClient(DeviceInfoConfiguration configuration, ILogger<DeviceInfoTableClient> logger) 
        : base(configuration, logger)
    {
    }
}
