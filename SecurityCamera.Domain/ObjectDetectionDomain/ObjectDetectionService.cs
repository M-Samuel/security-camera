using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;
using SecurityCamera.SharedKernel;


namespace SecurityCamera.Domain.ObjectDetectionDomain;

public class ObjectDetectionService
{
    private readonly IQueuePublisherService _queuePublisherService;
    private readonly IAiDetectionService _aiDetectionService;
    private readonly IObjectDetectionWriteRepository _objectDetectionWriteRepository;

    public ObjectDetectionService(
        IQueuePublisherService queuePublisherService,
        IAiDetectionService aiDetectionService,
        IObjectDetectionWriteRepository objectDetectionWriteRepository
        )
    {
        _queuePublisherService = queuePublisherService;
        _aiDetectionService = aiDetectionService;
        _objectDetectionWriteRepository = objectDetectionWriteRepository;
    }
    
    
    public async Task<Result<QueueMessage>> PushImageToQueue(DetectionEvent imageDetectedEvent, CancellationToken cancellationToken = default)
    {
        Result<QueueMessage> result = new Result<QueueMessage>(null);
        result
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageDetectedEvent.ImageName),
                new ArgumentError("ImageName Cannot Be null")
            )
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageDetectedEvent.CameraName),
                new ArgumentError("CameraName Cannot Be null")
            );
        QueueMessage queueMessage = new()
        {
            Body = imageDetectedEvent.ImageBytes,
            QueueMessageHeaders = new[]
            {
                new QueueMessageHeader("CameraName", imageDetectedEvent.CameraName),
                new QueueMessageHeader("CreatedUTCDateTime",
                    imageDetectedEvent.ImageCreatedDateTime.ToString("yyyyMMddHHmmssfff")),
                new QueueMessageHeader("DetectionType", imageDetectedEvent.DetectionType.ToString())
            },
            QueueName = imageDetectedEvent.CameraName,
        };
        bool messageSent = await _queuePublisherService.SentMessageToQueue(queueMessage, cancellationToken);
        result.AddErrorIf(() => !messageSent, new InvalidOperationError("Message not published to queue"));
        
        result.UpdateValueIfNoError(queueMessage);
        return result;
    }
    
    
    public async Task<Result<DetectionEvent?>> LaunchDetectionAlgorithm(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken = default)
    {
        Result<DetectionEvent?> result = new Result<DetectionEvent?>(null);
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
        
        DetectionEvent? detectionEvent = await _aiDetectionService.AnalyseImage(imageRecordedEvent, cancellationToken).FirstOrDefaultAsync(cancellationToken);
        result.UpdateValueIfNoError(detectionEvent);
    
        return result;
    }
    
    
    public async Task<Result<ImageDetection>> SaveDetectionToDb(DetectionEvent detectionEvent, CancellationToken cancellationToken = default)
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
            DetectionType = detectionEvent.DetectionType,
            Id = new Guid()
        };
        await _objectDetectionWriteRepository.SaveImageDetection(imageDetection, cancellationToken);

        result.UpdateValueIfNoError(imageDetection);

        return result;
    }
}