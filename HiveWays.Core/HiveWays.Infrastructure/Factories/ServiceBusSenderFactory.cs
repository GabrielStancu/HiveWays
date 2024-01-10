using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using HiveWays.Business.ServiceBusClient;
using HiveWays.Infrastructure.Clients;

namespace HiveWays.Infrastructure.Factories;

public class ServiceBusSenderFactory : IServiceBusSenderFactory
{
    private readonly ConcurrentDictionary<string, ServiceBusClient> _clients = new();
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private readonly object _lockObject = new();

    public IServiceBusSenderClient GetServiceBusSenderClient(string connectionString, string queueName)
    {
        var key = $"{connectionString}-{queueName}";

        if (SenderExistsAndIsOpen(key))
        {
            return new ServiceBusSenderClient(_senders[key]);
        }

        lock (_lockObject)
        {
            var client = GetServiceBusClient(connectionString);
            var sender = client.CreateSender(queueName);

            _senders[key] = sender;
        }

        return new ServiceBusSenderClient(_senders[key]);
    }

    private ServiceBusClient GetServiceBusClient(string connectionString)
    {
        lock (_lockObject)
        {
            if (ClientDoesNotExistOrIsClosed(connectionString))
            {
                var client = new ServiceBusClient(connectionString, new ServiceBusClientOptions
                {
                    TransportType = ServiceBusTransportType.AmqpTcp
                });

                _clients[connectionString] = client;
            }

            return _clients[connectionString];
        }
    }

    private bool ClientDoesNotExistOrIsClosed(string key)
    {
        return !_clients.ContainsKey(key) || _clients[key].IsClosed;
    }

    private bool SenderExistsAndIsOpen(string key)
    {
        return _senders.ContainsKey(key) && !_senders[key].IsClosed;
    }
}
