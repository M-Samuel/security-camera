using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public interface IImageRecorderService
{
    Task<Result<ImageRecordedEvent[]>> ScanDirectory(StartDirectoryScanEvent startDirectoryScanEvent, string cameraName, CancellationToken cancellationToken);
    Task<Result<QueueMessage>> PushImageToQueue(ImageRecordedEvent imageRecordedEvent, string queueName, CancellationToken cancellationToken);
}