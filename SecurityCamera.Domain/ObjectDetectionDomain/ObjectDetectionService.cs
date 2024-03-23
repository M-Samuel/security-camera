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

    
    public async Task<Result<DetectionEvent?>> LaunchDetectionAlgorithm(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken = default)
    {
        Result<DetectionEvent?> result = new Result<DetectionEvent?>(null);
        result
            .AddErrorIf(
                () => !File.Exists(imageRecordedEvent.TempLocalImagePath), 
                new ArgumentError("Image file does not exists"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.ImageName),
                new ArgumentError("ImageName cannot be empty"))
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.CameraName),
                new ArgumentError("CameraName cannot be empty"));
    
        if (result.HasError)
            return result;
        
        DetectionEvent? detectionEvent = _aiDetectionService.AnalyseImage(imageRecordedEvent, cancellationToken).FirstOrDefault();
        result.UpdateValueIfNoError(detectionEvent);
    
        return await Task.FromResult(result);
    }
    
    
    public async Task<Result<ImageDetection>> SaveDetectionToDb(DetectionEvent detectionEvent, string remoteStorageContainer, string remoteStorageFilePath, CancellationToken cancellationToken = default)
    {
        Result<ImageDetection> result = new Result<ImageDetection>(null);
        result
            .AddErrorIf(
                () => !File.Exists(detectionEvent.TempLocalImagePath), 
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
            ImageSize = new FileInfo(detectionEvent.TempLocalImagePath).Length,
            ImageName = detectionEvent.ImageName,
            DetectionDateTime = detectionEvent.ImageCreatedDateTime,
            DetectionType = detectionEvent.DetectionType,
            RemoteStorageContainer = remoteStorageContainer,
            RemoteStorageFilePath = remoteStorageFilePath,
            Id = new Guid()
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
            DetectionType = detectionEvent.DetectionType,
            CameraName = detectionEvent.CameraName,
            ImageCreatedDateTime = detectionEvent.ImageCreatedDateTime,
            ImageName = detectionEvent.ImageName,
            RemoteStorageContainer = remoteStorageContainer,
            RemoteStorageFilePath = remoteStorageFilePath
        };
        bool messageSent = await _queuePublisherService.SentMessageToQueue(queueMessage, cancellationToken);
        await _remoteStorageService.UploadRemoteStorageFile(remoteStorageContainer, remoteStorageFilePath, File.OpenRead(detectionEvent.TempLocalImagePath), cancellationToken);
        result.AddErrorIf(() => !messageSent, new InvalidOperationError("Message not published to queue"));
        result.UpdateValueIfNoError(queueMessage);
        
        File.Delete(detectionEvent.TempLocalImagePath);
        return result;
    }
}