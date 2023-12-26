namespace HiveWays.Business.EventGridMqttClient;

public interface IEventGridMqttClient<in T> where T : class
{
    Task SendEventAsync(T eventObject);
}