using SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public interface IImageRecorderService
{
    Task<Result<ImageDetectedEvent[]>> ScanDirectory(StartDirectoryScanEvent startDirectoryScanEvent, string cameraName);
    Task<Result<QueueMessage>> PushImageToQueue(ImageDetectedEvent imageDetectedEvent);
}