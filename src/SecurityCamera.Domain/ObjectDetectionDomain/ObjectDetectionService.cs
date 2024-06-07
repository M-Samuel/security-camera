using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;
using SecurityCamera.SharedKernel;


namespace SecurityCamera.Domain.ObjectDetectionDomain;

public class ObjectDetectionService : IObjectDetectionService
{
    private readonly IQueuePublisherService<DetectionMessage> _queuePublisherService;
    private readonly IAiDetectionService _aiDetectionService;
    private readonly IObjectDetectionWriteRepository _objectDetectionWriteRepository;
    private readonly IRemoteStorageService _remoteStorageService;

    public ObjectDetectionService(
        IQueuePublisherService<DetectionMessage> queuePublisherService,
        IAiDetectionService aiDetectionService,
        IObjectDetectionWriteRepository objectDetectionWriteRepository,
        IRemoteStorageService remoteStorageService
        )
    {
        _queuePublisherService = queuePublisherService;
        _aiDetectionService = aiDetectionService;
        _objectDetectionWriteRepository = objectDetectionWriteRepository;
        _remoteStorageService = remoteStorageService;
    }

    
    public async Task<Result<DetectionEvent[]?>> LaunchDetectionAlgorithm(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken = default)
    {
        Result<DetectionEvent[]?> result = new Result<DetectionEvent[]?>(null);
        result
            .AddErrorIf(
                () => imageRecordedEvent.ImageBytes.Length == 0, 
                new ArgumentError("ImageBytes cannot be empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.ImageName),
                new ArgumentError("ImageName cannot be empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.CameraName),
                new ArgumentError("CameraName cannot be empty"));
    
        if (result.HasError)
            return result;
        
        DetectionEvent[]? detectionEvents = await _aiDetectionService.AnalyseImage(imageRecordedEvent, cancellationToken);
        result.UpdateValueIfNoError(detectionEvents);
    
        return await Task.FromResult(result);
    }
    
    
    public async Task<Result<ImageDetection>> SaveDetectionToDb(DetectionEvent detectionEvent, string remoteStorageContainer, string remoteStorageFilePath, CancellationToken cancellationToken = default)
    {
        Result<ImageDetection> result = new Result<ImageDetection>(null);
        result
            .AddErrorIf(
                () => detectionEvent.ImageBytes.Length == 0, 
                new ArgumentError("ImageBytes cannot be empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(detectionEvent.ImageName),
                new ArgumentError("ImageName cannot be empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(detectionEvent.CameraName),
                new ArgumentError("CameraName cannot be empty"));

        if (result.HasError)
            return result;

        ImageDetection imageDetection = new(){
            CameraName = detectionEvent.CameraName,
            ImageSize = detectionEvent.ImageBytes.Length,
            ImageName = detectionEvent.ImageName,
            DetectionDateTime = detectionEvent.ImageCreatedDateTime,
            DetectionData = detectionEvent.DetectionData,
            RemoteStorageContainer = remoteStorageContainer,
            RemoteStorageFilePath = remoteStorageFilePath,
            Score = detectionEvent.Score,
            OriginX = detectionEvent.OriginX,
            OriginY = detectionEvent.OriginY,
            Width = detectionEvent.Width,
            Height = detectionEvent.Height,
            Id = Guid.NewGuid()
        };
        await _objectDetectionWriteRepository.SaveImageDetection(imageDetection, cancellationToken);

        result.UpdateValueIfNoError(imageDetection);

        return result;
    }


    public async Task<Result<DetectionMessage>> PushDetectionToQueue(string detectionQueue, DetectionEvent detectionEvent, string remoteStorageContainer, string remoteStorageFilePath, CancellationToken cancellationToken)
    {
        Result<DetectionMessage> result = new Result<DetectionMessage>(null);
        result
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(detectionEvent.ImageName),
                new ArgumentError("ImageName Cannot Be null")
            )
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(detectionEvent.CameraName),
                new ArgumentError("CameraName Cannot Be null")
            );
        DetectionMessage queueMessage = new()
        {
            QueueName = detectionQueue,
            DetectionData = detectionEvent.DetectionData,
            CameraName = detectionEvent.CameraName,
            ImageCreatedDateTime = detectionEvent.ImageCreatedDateTime,
            ImageName = detectionEvent.ImageName,
            RemoteStorageContainer = remoteStorageContainer,
            RemoteStorageFilePath = remoteStorageFilePath,
            Score = detectionEvent.Score,
            OriginX = detectionEvent.OriginX,
            OriginY = detectionEvent.OriginY,
            Width = detectionEvent.Width,
            Height = detectionEvent.Height,
        };
        bool messageSent = await _queuePublisherService.SentMessageToQueue(queueMessage, cancellationToken);
        await _remoteStorageService.UploadRemoteStorageFile(remoteStorageContainer, remoteStorageFilePath, detectionEvent.ImageBytes, cancellationToken);
        result.AddErrorIf(() => !messageSent, new InvalidOperationError("Message not published to queue"));
        
        result.UpdateValueIfNoError(queueMessage);
        return result;
    }
}