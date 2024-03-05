using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.InfrastructureServices;

public interface IQueuePublisherService
{
    Task<bool> SentMessageToQueue(QueueMessage queueMessage);
}