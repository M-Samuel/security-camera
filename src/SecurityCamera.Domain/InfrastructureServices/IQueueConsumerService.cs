using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.InfrastructureServices;

public interface IQueueConsumerService<T> : IDisposable where T : QueueMessage
{
    event EventHandler<T> MessageReceived;
    Task GetMessageFromQueue(string queueName, EventHandler<T> subscriber, CancellationToken cancellationToken);
}