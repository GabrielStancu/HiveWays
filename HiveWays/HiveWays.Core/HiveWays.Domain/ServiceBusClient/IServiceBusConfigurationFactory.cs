namespace HiveWays.Business.ServiceBusClient;

public interface IServiceBusConfigurationFactory
{
    ServiceBusConfiguration GetServiceBusConfiguration(string key);
}