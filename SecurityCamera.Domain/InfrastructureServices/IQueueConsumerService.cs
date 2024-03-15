namespace SecurityCamera.Domain.InfrastructureServices;

public interface IQueueConsumerService<T> : IDisposable where T : QueueMessage
{
    Task GetMessageFromQueue(string queueName, Action<T> onMessageReceived, CancellationToken cancellationToken);
}