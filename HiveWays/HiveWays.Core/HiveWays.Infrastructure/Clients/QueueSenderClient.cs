using System.Text.Json;
using HiveWays.Business.ServiceBusClient;
using Azure.Messaging.ServiceBus;
using HiveWays.Business.Extensions;
using Microsoft.Extensions.Logging;

namespace HiveWays.Infrastructure.Clients;

public class QueueSenderClient<T> : IQueueSenderClient<T> where T : class
{
    private readonly ServiceBusConfiguration _configuration;
    private readonly ILogger<QueueSenderClient<T>> _logger;
    private ServiceBusSender _sender;

    public QueueSenderClient(ServiceBusConfiguration configuration, ILogger<QueueSenderClient<T>> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendMessagesBatchedAsync(IEnumerable<T> messages)
    {
        InitClient();

        var batches = messages.Batch(_configuration.BatchSize);
        foreach (var batch in batches)
        {
            var messageBatch = await CreateServiceBusBatchAsync(batch);

            try
            {
                await _sender.SendMessagesAsync(messageBatch);
                _logger.LogInformation("A batch of {ServiceBusBatchSize} has been published to the queue.", _configuration.BatchSize);
            }
            catch (Exception ex)
            {
                _logger.LogError("Encountered error while sending a batch to service bus. " +
                                 "Exception: {ServiceBusBatchSendException} @ {ServiceBusBatchExceptionStackTrace}", ex.Message, ex.StackTrace);
                throw;
            }
        }
    }

    public async Task SendMessageAsync(T message)
    {
        InitClient();

        try
        {
            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(message));
            await _sender.SendMessageAsync(serviceBusMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError("Encountered error while sending a message to service bus. " +
                             "Exception: {ServiceBusSendException} @ {ServiceBusExceptionStackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    private async Task<ServiceBusMessageBatch> CreateServiceBusBatchAsync(IEnumerable<T> batch)
    {
        using var messageBatch = await _sender.CreateMessageBatchAsync();

        foreach (var message in batch)
        {
            if (!messageBatch.TryAddMessage(new ServiceBusMessage(JsonSerializer.Serialize(message))))
            {
                throw new Exception($"Could not fit message in the batch");
            }
        }

        return messageBatch;
    }

    private void InitClient()
    {
        if (_sender != null)
            return;

        var client = new ServiceBusClient(_configuration.ConnectionString, new ServiceBusClientOptions());
        
        _sender = client.CreateSender(_configuration.QueueName);
        _logger.LogInformation("Initialized service bus sender");
    }
}
