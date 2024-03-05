using System.Text;
using SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainErrors;
using SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public class ImageRecorderService : IImageRecorderService
{
    private readonly IQueuePublisherService _queuePublisherService;

    public ImageRecorderService(IQueuePublisherService queuePublisherService)
    {
        _queuePublisherService = queuePublisherService;
    }
    public async Task<Result<ImageDetectedEvent[]>> ScanDirectory(StartDirectoryScanEvent startDirectoryScanEvent, string cameraName)
    {
        Result<ImageDetectedEvent[]> result = new Result<ImageDetectedEvent[]>(null);
        result
            .AddErrorIf(
                () => string.IsNullOrWhiteSpace(startDirectoryScanEvent.Directory)
                , new ArgumentError("Scan directory could not be null/empty"))    
            .AddErrorIf(
                () => Directory.Exists(startDirectoryScanEvent.Directory)
                , new ScanDirectoryDoesNotExistsError($"{startDirectoryScanEvent.Directory} does not exists"));

        if (result.HasError)
            return result;

        string[] detectedFiles = Directory.EnumerateFiles(startDirectoryScanEvent.Directory, "*.png").ToArray();
        ImageDetectedEvent[] imageDetectedEvents  = await ConvertToImageDetectionEvent(detectedFiles, cameraName).ToArrayAsync();
        
        result.UpdateValueIfNoError(imageDetectedEvents);

        return result;
    }

    private async IAsyncEnumerable<ImageDetectedEvent> ConvertToImageDetectionEvent(string[] detectedFiles, string cameraName)
    {
        foreach (string filePath in detectedFiles)
        {
            ImageDetectedEvent imageDetectedEvent = new ImageDetectedEvent(
                DateTime.UtcNow,
                cameraName,
                await File.ReadAllBytesAsync(filePath),
                Path.GetFileName(filePath),
                new FileInfo(filePath).CreationTimeUtc
            );
            yield return imageDetectedEvent;
        }
    }

    public async Task<Result<QueueMessage>> PushImageToQueue(ImageDetectedEvent imageDetectedEvent)
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
            Body = Convert.ToBase64String(imageDetectedEvent.ImageBytes),
            QueueMessageHeaders = new[]
            {
                new QueueMessageHeader("CameraName", imageDetectedEvent.CameraName),
                new QueueMessageHeader("CreatedUTCDateTime",
                    imageDetectedEvent.ImageCreatedDateTime.ToString("yyyyMMddHHmmssfff"))
            },
            QueueName = imageDetectedEvent.CameraName
        };
        bool messageSent = await _queuePublisherService.SentMessageToQueue(queueMessage);
        result.AddErrorIf(() => !messageSent, new InvalidOperationError("Message not published to queue"));
        
        result.UpdateValueIfNoError(queueMessage);
        return result;
    }
}