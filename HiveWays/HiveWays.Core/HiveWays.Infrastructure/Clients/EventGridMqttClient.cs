using System.Text.Json;
using Azure;
using Azure.Messaging.EventGrid;
using HiveWays.Business.EventGridMqttClient;
using Microsoft.Extensions.Logging;

namespace HiveWays.Infrastructure.Clients;

public class EventGridMqttClient<T> : IEventGridMqttClient<T> where T : class
{
    private readonly ClientConfiguration _configuration;
    private readonly ILogger<EventGridMqttClient<T>> _logger;
    private EventGridPublisherClient _client;

    public EventGridMqttClient(ClientConfiguration configuration, ILogger<EventGridMqttClient<T>> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEventAsync(T eventObject)
    {
        InitClient();

        var carEvent = new EventGridEvent(_configuration.Subject, _configuration.Event, _configuration.Version, eventObject);
        var serializedData = JsonSerializer.Serialize(eventObject);

        await _client.SendEventAsync(carEvent);
        _logger.LogInformation("Sent event for {EventData}", serializedData);
    }

    private void InitClient()
    {
        if (_client != null)
            return;

        var endpoint = new Uri(_configuration.Uri);
        var credentials = new AzureKeyCredential(_configuration.Key);
        _client = new EventGridPublisherClient(endpoint, credentials);
    }
}
