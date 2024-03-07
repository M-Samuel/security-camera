namespace SecurityCamera.Domain.InfrastructureServices;

public interface IQueueConsumerService : IDisposable
{
    Task GetMessageFromQueue(string queueName, Action<QueueMessage> onMessageReceived, CancellationToken cancellationToken);
}