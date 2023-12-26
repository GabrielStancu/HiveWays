namespace HiveWays.Business.ServiceBusClient;

public interface IQueueSenderClient<in T> where T : class
{
    Task SendMessagesBatchedAsync(IEnumerable<T> messages);
    Task SendMessageAsync(T message);
}