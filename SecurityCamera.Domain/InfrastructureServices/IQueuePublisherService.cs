using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.InfrastructureServices;

public interface IQueuePublisherService : IDisposable
{
    Task<bool> SentMessageToQueue(QueueMessage queueMessage, CancellationToken cancellationToken);
}