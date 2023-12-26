namespace HiveWays.Business.ServiceBusClient;

public class ServiceBusConfiguration
{
    public string ConnectionString { get; set; }
    public string QueueName { get; set; }
    public int BatchSize { get; set; }
}
