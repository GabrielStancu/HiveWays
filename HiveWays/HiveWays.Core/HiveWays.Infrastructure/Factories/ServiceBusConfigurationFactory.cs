using HiveWays.Business.ServiceBusClient;

namespace HiveWays.Infrastructure.Factories;

public class ServiceBusConfigurationFactory : IServiceBusConfigurationFactory
{
    private readonly IEnumerable<ServiceBusConfiguration> _configurations;

    public ServiceBusConfigurationFactory(IEnumerable<ServiceBusConfiguration> configurations)
    {
        _configurations = configurations;
    }

    public ServiceBusConfiguration GetServiceBusConfiguration(string key)
    {
        var configuration = _configurations.FirstOrDefault(c => c.Key == key);

        if (configuration is null)
        {
            throw new InvalidOperationException($"No configuration with the provided key <{key}> is registered");
        }

        return configuration;
    }
}
