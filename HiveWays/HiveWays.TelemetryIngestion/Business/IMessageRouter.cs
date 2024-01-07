using HiveWays.TelemetryIngestion.Configuration;

namespace HiveWays.TelemetryIngestion.Business;

public interface IMessageRouter
{
    string GetRoutingQueue(ServiceBusMessageType messageType);
}