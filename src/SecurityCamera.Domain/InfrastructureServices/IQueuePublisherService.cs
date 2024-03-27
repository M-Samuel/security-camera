using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.InfrastructureServices;

public interface IQueuePublisherService<T> : IDisposable where T : QueueMessage
{
    Task<bool> SentMessageToQueue(T queueMessage, CancellationToken cancellationToken);
}