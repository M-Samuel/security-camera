using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.InfrastructureServices;

public class QueueMessage
{
    public required string QueueName { get; set; }
    public QueueMessageHeader[]? QueueMessageHeaders { get; set; }
    public required byte[] Body { get; set; }
}

