using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public interface IImageRecorderService
{
    Task<Result<ImageRecordedEvent[]>> ScanDirectory(StartDirectoryScanEvent startDirectoryScanEvent, string cameraName, CancellationToken cancellationToken);
    Task<Result<QueueMessage>> PushImagePathToQueue(ImageRecordedEvent imageRecordedEvent, string queueName, string remoteStorageContainer, string remoteStorageFilePath, CancellationToken cancellationToken);
    Task<Result<ImageRecordedEvent>> SaveImageToRemoteStorage(ImageRecordedEvent imageRecordedEvent, string remoteStorageContainer, string remoteStorageFilePath, CancellationToken cancellationToken);
}