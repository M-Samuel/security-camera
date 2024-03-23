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
    private ObjectDetectionCommandData? _commandData;
    private EventId _eventId;
    private CancellationToken _cancellationToken;
    private EventHandler<ImageRecorderOnImagePushMessage>? _eventHandler;

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
        _commandData = commandData;
        _eventId = eventId;
        _cancellationToken = cancellationToken;
        _eventHandler = Handle;
        
        
        await _queueConsumerService.GetMessageFromQueue(
            commandData.ImageQueue,
            _eventHandler,
            cancellationToken);
        
        
        return await Task.FromResult(new ObjectDetectionCommandResult());
    }

    public void Handle(object? sender, ImageRecorderOnImagePushMessage queueMessage)
    {
        Task.Factory.StartNew(async () => await ProcessMessage(queueMessage), _cancellationToken);
    }

    private async Task ProcessMessage(ImageRecorderOnImagePushMessage queueMessage)
    {
        try
        {
            if (_commandData == null)
                return;
            if(string.IsNullOrWhiteSpace(queueMessage.RemoteStorageFilePath))
                return;
            
            string remoteStorageFilePath = queueMessage.RemoteStorageFilePath;

            RemoteStorageFile remoteStorageFile =
                await _remoteStorageService.DownloadRemoteStorageFile(_commandData.RemoteStorageContainer,
                    remoteStorageFilePath, _cancellationToken);
            if (remoteStorageFile.FileContent == null || remoteStorageFile.FileContent.Length == 0)
            {
                _logger.LogError(_eventId,
                    $"No image content at location:{_commandData.RemoteStorageContainer} {_commandData.RemoteStorageFileDirectory}");
                return;
            }

            ImageRecordedEvent imageRecordedEvent = new(
                OccurrenceDateTime: DateTime.Now,
                CameraName: queueMessage.CameraName ?? "",
                ImageBytes: remoteStorageFile.FileContent,
                ImageCreatedDateTime: queueMessage.ImageCreatedDateTime,
                ImageName: queueMessage.ImageName ?? ""
            );

            var detectionResult =
                await _objectDetectionService.LaunchDetectionAlgorithm(imageRecordedEvent, _cancellationToken);
            if (detectionResult.HasError)
            {
                _logger.ProcessApplicationErrors(detectionResult.DomainErrors, _eventId);
                return;
            }

            if (detectionResult.Value == null)
            {
                _logger.LogInformation(_eventId, "No detection result");
                return;
            }

            var saveResult = await _objectDetectionService.SaveDetectionToDb(detectionResult.Value,
                _commandData.RemoteStorageContainer, remoteStorageFilePath, _cancellationToken);
            if (saveResult.HasError)
                _logger.ProcessApplicationErrors(saveResult.DomainErrors, _eventId);
            else
            {
                var pushResult = await _objectDetectionService.PushDetectionToQueue(_commandData.DetectionQueue,
                    detectionResult.Value, _commandData.RemoteStorageContainer, remoteStorageFilePath,
                    _cancellationToken);
                if (pushResult.HasError)
                    _logger.ProcessApplicationErrors(pushResult.DomainErrors, _eventId);
            }
        }
        finally
        {
            _queueConsumerService.MessageReceived -= _eventHandler;
            
        }
        
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