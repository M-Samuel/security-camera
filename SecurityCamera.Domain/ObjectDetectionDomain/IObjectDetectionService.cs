using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ObjectDetectionDomain;

public interface IObjectDetectionService
{
    Task<Result<DetectionEvent?>> LaunchDetectionAlgorithm(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken);
    Task<Result<ImageDetection>> SaveDetectionToDb(DetectionEvent detectionEvent, CancellationToken cancellationToken);
    Task<Result<QueueMessage>> PushImageToQueue(DetectionEvent detectionEvent, CancellationToken cancellationToken);
}