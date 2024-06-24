using HiveWays.FleetIntegration.Business.Configuration;
using HiveWays.FleetIntegration.Business.Interfaces;
using HiveWays.FleetIntegration.Models;
using HiveWays.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace HiveWays.FleetIntegration.Business.Clients;

public class RoutingInfoTableClient : TableStorageClient<RoutingInfoEntity>, IRoutingInfoTableClient
{
    public RoutingInfoTableClient(RoutingInfoConfiguration configuration, ILogger<RoutingInfoTableClient> logger)
        : base(configuration, logger)
    {
    }
}
