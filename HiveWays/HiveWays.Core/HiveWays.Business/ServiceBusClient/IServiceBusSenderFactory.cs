using HiveWays.Business.ServiceBusClient;

namespace HiveWays.Infrastructure.Factories;

public interface IServiceBusSenderFactory
{
    IServiceBusSenderClient GetServiceBusSenderClient(string connectionString, string queueName);
}