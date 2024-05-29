using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.InfrastructureServices;

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

public interface IQueueConsumerService<T> : IDisposable where T : QueueMessage
{
    event AsyncEventHandler<T> MessageReceived;
    Task GetMessageFromQueue(string queueName, AsyncEventHandler<T> subscriber, CancellationToken cancellationToken);
    Task GetMessageFromQueue(string queueName, AsyncEventHandler<T> subscriber, int maxConcurrent, CancellationToken cancellationToken);
    Task GetMessageFromQueue(string queueName, AsyncEventHandler<T> subscriber, int maxConcurrent, int maxCount, CancellationToken cancellationToken);
}