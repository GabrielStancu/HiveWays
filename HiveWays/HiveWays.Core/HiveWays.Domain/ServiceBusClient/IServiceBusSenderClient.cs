namespace HiveWays.Business.ServiceBusClient;

public interface IServiceBusSenderClient
{
    Task SendMessageAsync<T>(T message);
}