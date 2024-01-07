using HiveWays.TelemetryIngestion.Configuration;

namespace HiveWays.TelemetryIngestion.Business;

public class MessageRouter : IMessageRouter
{
    private readonly RoutingServiceBusConfiguration _routingServiceBusConfiguration;

    public MessageRouter(RoutingServiceBusConfiguration routingServiceBusConfiguration)
    {
        _routingServiceBusConfiguration = routingServiceBusConfiguration;
    }

    public string GetRoutingQueue(ServiceBusMessageType messageType)
    {
        switch (messageType)
        {
            case ServiceBusMessageType.AlertReceived:
                return _routingServiceBusConfiguration.AlertQueueName;
            case ServiceBusMessageType.StatusReceived:
                return _routingServiceBusConfiguration.StatusQueueName;
            case ServiceBusMessageType.TripReceived:
                return _routingServiceBusConfiguration.TripQueueName;
            default:
                throw new NotImplementedException($"Message of type {messageType} is not supported");
        }
    }
}
