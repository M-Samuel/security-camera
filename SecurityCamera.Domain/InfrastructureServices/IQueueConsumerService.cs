namespace SecurityCamera.Domain.InfrastructureServices;

public interface IQueueConsumerService
{
    Task GetMessageFromQueue(string queueName, Action<QueueMessage> onMessageReceived, CancellationToken cancellationToken);
}