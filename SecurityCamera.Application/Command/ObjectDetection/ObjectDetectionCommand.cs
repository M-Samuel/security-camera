using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Application.Command.ObjectDetection;

public class ObjectDetectionCommand : ICommand<ObjectDetectionCommandData, ObjectDetectionCommandResult>
{
    private readonly ILogger<ObjectDetectionCommand> _logger;
    private readonly IObjectDetectionService _objectDetectionService;
    private readonly IQueueConsumerService<ImageRecorderOnImagePushMessage> _queueConsumerService;
    private readonly IRemoteStorageService _remoteStorageService;

    public ObjectDetectionCommand(
        ILogger<ObjectDetectionCommand> logger,
        IObjectDetectionService objectDetectionService,
        IQueueConsumerService<ImageRecorderOnImagePushMessage> queueConsumerService,
        IRemoteStorageService remoteStorageService
    )
    {
        _logger = logger;
        _objectDetectionService = objectDetectionService;
        _queueConsumerService = queueConsumerService;
        _remoteStorageService = remoteStorageService;
    }

    public async Task<ObjectDetectionCommandResult> ProcessCommandAsync(ObjectDetectionCommandData commandData, EventId eventId, CancellationToken cancellationToken)
    {
        await _queueConsumerService.GetMessageFromQueue(
            commandData.ImageQueue,
            async (queueMessage) => {
                string remoteStorageFilePath = $"{commandData.RemoteStorageFileDirectory}/{queueMessage.ImageName}";

                RemoteStorageFile remoteStorageFile = await _remoteStorageService.DownloadRemoteStorageFile(commandData.RemoteStorageContainer, remoteStorageFilePath, cancellationToken);
                if(remoteStorageFile.FileContent == null || remoteStorageFile.FileContent.Length == 0)
                {
                    _logger.LogError(eventId, $"No image content at location:{commandData.RemoteStorageContainer} {commandData.RemoteStorageFileDirectory}");
                    return;
                }

                ImageRecordedEvent imageRecordedEvent = new(
                    OccurrenceDateTime: DateTime.Now,
                    CameraName: queueMessage.CameraName ?? "",
                    ImageBytes: remoteStorageFile.FileContent,
                    ImageCreatedDateTime: queueMessage.ImageCreatedDateTime,
                    ImageName: queueMessage.ImageName  ?? ""
                );

                var detectionResult = await _objectDetectionService.LaunchDetectionAlgorithm(imageRecordedEvent, cancellationToken);
                if(detectionResult.HasError)
                {
                    _logger.ProcessApplicationErrors(detectionResult.DomainErrors, eventId);
                    return;
                }
                if(detectionResult.Value == null)
                {
                    _logger.LogInformation(eventId, "No detection result");
                    return;
                }

                var saveResult = await _objectDetectionService.SaveDetectionToDb(detectionResult.Value, commandData.RemoteStorageContainer, remoteStorageFilePath, cancellationToken);
                if(saveResult.HasError)
                    _logger.ProcessApplicationErrors(saveResult.DomainErrors, eventId);
                else
                {
                    var pushResult = await _objectDetectionService.PushDetectionToQueue(commandData.DetectionQueue, detectionResult.Value, commandData.RemoteStorageContainer, remoteStorageFilePath, cancellationToken);
                    if(pushResult.HasError)
                        _logger.ProcessApplicationErrors(pushResult.DomainErrors, eventId);
                }
            },
            cancellationToken);

        return await Task.FromResult(new ObjectDetectionCommandResult());
    }
}

public class ObjectDetectionCommandResult
{
}

public class ObjectDetectionCommandData
{
    public required string ImageQueue { get; set; }
    public required string DetectionQueue { get; set; }
    
    public required string RemoteStorageContainer { get; set; }
    public required string RemoteStorageFileDirectory { get; set; }
}