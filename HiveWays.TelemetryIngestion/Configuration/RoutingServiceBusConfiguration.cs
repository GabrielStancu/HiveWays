namespace HiveWays.TelemetryIngestion.Configuration;

public class RoutingServiceBusConfiguration
{
    public string ConnectionString { get; set; }
    public string StatusQueueName { get; set; }
    public string AlertQueueName { get; set; }
}
