using SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public interface IImageRecorderService
{
    Task<Result<ImageRecordedEvent[]>> ScanDirectory(StartDirectoryScanEvent startDirectoryScanEvent, string cameraName, CancellationToken cancellationToken);
    Task<Result<DetectionEvent?>> LauchDectectionAlogirthm(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken);
    Task<Result<ImageDetection>> SaveDetectionToDB(DetectionEvent detectionEvent, CancellationToken cancellationToken);
    Task<Result<QueueMessage>> PushImageToQueue(DetectionEvent detectionEvent, CancellationToken cancellationToken);
}