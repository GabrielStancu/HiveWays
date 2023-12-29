using System.Text.Json;
using HiveWays.Business.ServiceBusClient;
using Azure.Messaging.ServiceBus;

namespace HiveWays.Infrastructure.Clients;

public class ServiceBusSenderClient : IServiceBusSenderClient
{
    private readonly ServiceBusSender _sender;

    public ServiceBusSenderClient(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public async Task SendMessageAsync<T>(T message)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(messageJson);

        await _sender.SendMessageAsync(serviceBusMessage);
    }
}
