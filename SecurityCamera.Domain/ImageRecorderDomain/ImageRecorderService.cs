using System.Runtime.CompilerServices;
using SecurityCamera.Domain.ImageRecorderDomain.Errors;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public class ImageRecorderService : IImageRecorderService
{
    private readonly IQueuePublisherService<ImageRecorderOnImagePushMessage> _queuePublisherService;
    private readonly IRemoteStorageService _remoteStorageService;

    public ImageRecorderService(
        IQueuePublisherService<ImageRecorderOnImagePushMessage> queuePublisherService,
        IRemoteStorageService remoteStorageService)
    {
        _queuePublisherService = queuePublisherService;
        _remoteStorageService = remoteStorageService;
    }
    public async Task<Result<ImageRecordedEvent[]>> ScanDirectory(StartDirectoryScanEvent startDirectoryScanEvent, string cameraName, CancellationToken cancellationToken = default)
    {
        Result<ImageRecordedEvent[]> result = new Result<ImageRecordedEvent[]>(null);
        result
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(startDirectoryScanEvent.Directory)
                , new ArgumentError("Scan directory could not be null/empty"))    
            .AddErrorIf(
                () => !Directory.Exists(startDirectoryScanEvent.Directory)
                , new ScanDirectoryDoesNotExistsError($"Directory {startDirectoryScanEvent.Directory} does not exists"));

        if (result.HasError)
            return result;

        string[] recordedFiles = 
        Directory.EnumerateFiles(startDirectoryScanEvent.Directory, "*.png")
        .Concat(
            Directory.EnumerateFiles(startDirectoryScanEvent.Directory, "*.jpg")
        ).ToArray();
        ImageRecordedEvent[] imageRecordedEvents  = await ConvertToImageDetectionEvent(recordedFiles, cameraName, cancellationToken).ToArrayAsync(cancellationToken);
        result.UpdateValueIfNoError(imageRecordedEvents);

        return result;
    }

    private async IAsyncEnumerable<ImageRecordedEvent> ConvertToImageDetectionEvent(string[] recordedFiles, string cameraName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (string filePath in recordedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ImageRecordedEvent imageDetectedEvent = new ImageRecordedEvent(
                DateTime.UtcNow,
                cameraName,
                filePath,
                Path.GetFileName(filePath),
                new FileInfo(filePath).CreationTimeUtc
            );
            yield return await Task.FromResult(imageDetectedEvent);
        }
    }
    
    public async Task<Result<QueueMessage>> PushImagePathToQueue(ImageRecordedEvent imageRecordedEvent, string queueName, string remoteStorageContainer, string remoteStorageFilePath, CancellationToken cancellationToken = default)
    {
        Result<QueueMessage> result = new Result<QueueMessage>(null);
        ImageRecorderOnImagePushMessage queueMessage = new()
        {
            QueueName = queueName,
            ImageCreatedDateTime = imageRecordedEvent.ImageCreatedDateTime,
            ImageName = imageRecordedEvent.ImageName,
            CameraName = imageRecordedEvent.CameraName,
            RemoteStorageContainer = remoteStorageContainer,
            RemoteStorageFilePath = remoteStorageFilePath
        };
        bool messageSent = await _queuePublisherService.SentMessageToQueue(queueMessage, cancellationToken);
        result.AddErrorIf(() => !messageSent, new InvalidOperationError("Message not published to queue"));
        
        result.UpdateValueIfNoError(queueMessage);
        return result;
    }

    public async Task<Result<ImageRecordedEvent>> SaveImageToRemoteStorage(ImageRecordedEvent imageRecordedEvent, string remoteStorageContainer, string remoteStorageFilePath, CancellationToken cancellationToken)
    {
        Result<ImageRecordedEvent> result = new Result<ImageRecordedEvent>(imageRecordedEvent);
        result
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(remoteStorageContainer), 
                new ArgumentError("RemoteStorageContainer cannot be null/empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(remoteStorageFilePath),
                new ArgumentError("remoteStorageFilePath cannot be null/empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.ImageName),
                new ArgumentError("ImageName cannot be null/empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.TempLocalImagePath) || !File.Exists(imageRecordedEvent.TempLocalImagePath),
                new ArgumentError("TempLocalImagePath does not exists or is not given"));
        
        if(result.HasError)
            return result;

        await _remoteStorageService.CreateRemoteStorageContainer(remoteStorageContainer, cancellationToken);
        await _remoteStorageService.UploadRemoteStorageLargeFile(remoteStorageContainer, remoteStorageFilePath, File.OpenRead(imageRecordedEvent.TempLocalImagePath), cancellationToken);
        
        File.Delete(imageRecordedEvent.TempLocalImagePath);
        return result;
    }
}