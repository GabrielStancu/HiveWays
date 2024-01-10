using HiveWays.Domain.Entities;
using HiveWays.TelemetryIngestion.Configuration;

namespace HiveWays.TelemetryIngestion.Business;

public class RoutingData
{
    public IEnumerable<DataPointEntity> DataPointEntities { get; set; }
    public ServiceBusMessageType MessageType { get; set; }
}
