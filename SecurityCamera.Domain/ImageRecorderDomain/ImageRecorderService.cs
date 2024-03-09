using System.Runtime.CompilerServices;
using SecurityCamera.Domain.ImageRecorderDomain.Errors;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public class ImageRecorderService : IImageRecorderService
{
    private readonly IQueuePublisherService _queuePublisherService;

    public ImageRecorderService(
        IQueuePublisherService queuePublisherService)
    {
        _queuePublisherService = queuePublisherService;
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
        DeleteImages(recordedFiles);
        result.UpdateValueIfNoError(imageRecordedEvents);

        return result;
    }

    private static void DeleteImages(string[] recordedFiles)
    {
        foreach (string filePath in recordedFiles)
            File.Delete(filePath);
    }

    private async IAsyncEnumerable<ImageRecordedEvent> ConvertToImageDetectionEvent(string[] recordedFiles, string cameraName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (string filePath in recordedFiles)
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
    
}