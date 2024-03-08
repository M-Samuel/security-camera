using System.Runtime.CompilerServices;
using SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainErrors;
using SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;
using SecurityCamera.Domain.ImageRecorderDomain.Repository;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public class ImageRecorderService : IImageRecorderService
{
    private readonly IQueuePublisherService _queuePublisherService;
    // private readonly IAIDectectionService _aIDectectionService;
    private readonly IImageRecorderWriteRepository _imageRecorderWriteRepository;

    public ImageRecorderService(
        IQueuePublisherService queuePublisherService,
        // IAIDectectionService aIDectectionService,
        IImageRecorderWriteRepository imageRecorderWriteRepository)
    {
        _queuePublisherService = queuePublisherService;
        // _aIDectectionService = aIDectectionService;
        _imageRecorderWriteRepository = imageRecorderWriteRepository;
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

        string[] detectedFiles = 
        Directory.EnumerateFiles(startDirectoryScanEvent.Directory, "*.png")
        .Concat(
            Directory.EnumerateFiles(startDirectoryScanEvent.Directory, "*.jpg")
        ).ToArray();
        ImageRecordedEvent[] imageDetectedEvents  = await ConvertToImageDetectionEvent(detectedFiles, cameraName, cancellationToken).ToArrayAsync(cancellationToken);
        
        result.UpdateValueIfNoError(imageDetectedEvents);

        return result;
    }

    private async IAsyncEnumerable<ImageRecordedEvent> ConvertToImageDetectionEvent(string[] detectedFiles, string cameraName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (string filePath in detectedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ImageRecordedEvent imageDetectedEvent = new ImageRecordedEvent(
                DateTime.UtcNow,
                cameraName,
                await File.ReadAllBytesAsync(filePath, cancellationToken),
                Path.GetFileName(filePath),
                new FileInfo(filePath).CreationTimeUtc
            );
            yield return imageDetectedEvent;
        }
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
    
    public async Task<Result<QueueMessage>> PushImageToQueue(ImageRecordedEvent imageRecordedEvent, string queueName, CancellationToken cancellationToken = default)
    {
        Result<QueueMessage> result = new Result<QueueMessage>(null);
        result
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.ImageName),
                new ArgumentError("ImageName Cannot Be null")
            )
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(imageRecordedEvent.CameraName),
                new ArgumentError("CameraName Cannot Be null")
            );
        QueueMessage queueMessage = new()
        {
            Body = imageRecordedEvent.ImageBytes,
            QueueMessageHeaders = new[]
            {
                new QueueMessageHeader("CameraName", imageRecordedEvent.CameraName),
                new QueueMessageHeader("CreatedUTCDateTime",
                    imageRecordedEvent.ImageCreatedDateTime.ToString("yyyyMMddHHmmssfff"))
            },
            QueueName = queueName,
        };
        bool messageSent = await _queuePublisherService.SentMessageToQueue(queueMessage, cancellationToken);
        result.AddErrorIf(() => !messageSent, new InvalidOperationError("Message not published to queue"));
        
        result.UpdateValueIfNoError(queueMessage);
        return result;
    }

    // public async Task<Result<DetectionEvent?>> LaunchDetectionAlgorithm(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken = default)
    // {
    //     Result<DetectionEvent?> result = new Result<DetectionEvent?>(null);
    //     result
    //         .AddErrorIf(
    //             () => imageRecordedEvent.ImageBytes.Length == 0, 
    //             new ArgumentError("ImageBytes cannot be empty"))
    //         .AddErrorIf(
    //             () => string.IsNullOrWhiteSpace(imageRecordedEvent.ImageName),
    //             new ArgumentError("ImageName cannot be empty"))
    //         .AddErrorIf(
    //             () => string.IsNullOrWhiteSpace(imageRecordedEvent.CameraName),
    //             new ArgumentError("CameraName cannot be empty"));
    //
    //     if (result.HasError)
    //         return result;
    //     
    //     DetectionEvent? detectionEvent = await _aIDectectionService.AnalyseImage(imageRecordedEvent, cancellationToken);
    //     result.UpdateValueIfNoError(detectionEvent);
    //
    //     return result;
    // }

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
        await _imageRecorderWriteRepository.SaveImageDetection(imageDetection, cancellationToken);

        result.UpdateValueIfNoError(imageDetection);

        return result;
    }
}