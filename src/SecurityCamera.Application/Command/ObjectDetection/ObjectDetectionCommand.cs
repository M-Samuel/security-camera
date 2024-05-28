using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Application.Command.ObjectDetection;

public class ObjectDetectionCommand : ICommand<ObjectDetectionCommandData, ObjectDetectionCommandResult>
{
    private readonly ILogger<ObjectDetectionCommand> _logger;
    private readonly IQueueConsumerService<ImageRecorderOnImagePushMessage> _queueConsumerService;
    private readonly IRemoteStorageService _remoteStorageService;
    private ObjectDetectionCommandData? _commandData;
    private CancellationToken _cancellationToken;
    private EventHandler<ImageRecorderOnImagePushMessage>? _eventHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ObjectDetectionCommand(
        ILogger<ObjectDetectionCommand> logger,
        IQueueConsumerService<ImageRecorderOnImagePushMessage> queueConsumerService,
        IRemoteStorageService remoteStorageService,
        IServiceScopeFactory serviceScopeFactory
    )
    {
        _logger = logger;
        _queueConsumerService = queueConsumerService;
        _remoteStorageService = remoteStorageService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<ObjectDetectionCommandResult> ProcessCommandAsync(ObjectDetectionCommandData commandData, EventId eventId, CancellationToken cancellationToken)
    {
        _commandData = commandData;
        _cancellationToken = cancellationToken;
        _eventHandler = Handle;
        
        
        await _queueConsumerService.GetMessageFromQueue(
            commandData.ImageQueue,
            _eventHandler,
            maxConcurrent:1,
            cancellationToken);

        return await Task.FromResult(new ObjectDetectionCommandResult());
    }

    public void Handle(object? sender, ImageRecorderOnImagePushMessage queueMessage)
    {
        Task.Run(() => ProcessMessageAsync(queueMessage), _cancellationToken).GetAwaiter().GetResult();
    }

    private async Task ProcessMessageAsync(ImageRecorderOnImagePushMessage queueMessage)
    { 
        EventId eventId = new EventId((int)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds, Guid.NewGuid().ToString());
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            IObjectDetectionService objectDetectionService =
                scope.ServiceProvider.GetRequiredService<IObjectDetectionService>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            if (_commandData == null)
                return;
            if (string.IsNullOrWhiteSpace(queueMessage.RemoteStorageFilePath))
                return;

            string remoteStorageFilePath = queueMessage.RemoteStorageFilePath;

            RemoteStorageFile remoteStorageFile =
                await _remoteStorageService.DownloadRemoteStorageFile(_commandData.RemoteStorageContainer,
                    remoteStorageFilePath, _cancellationToken);
            if (remoteStorageFile.FileContent == null || remoteStorageFile.FileContent.Length == 0)
            {
                _logger.LogError(eventId,
                    $"No image content at location:{_commandData.RemoteStorageContainer} {_commandData.RemoteStorageFileDirectory}");
                return;
            }

            ImageRecordedEvent imageRecordedEvent = new(
                OccurrenceDateTime: DateTime.Now,
                CameraName: queueMessage.CameraName ?? string.Empty,
                ImageBytes: remoteStorageFile.FileContent,
                ImageCreatedDateTime: queueMessage.ImageCreatedDateTime,
                ImageName: queueMessage.ImageName ?? string.Empty
            );

            var detectionResult =
                await objectDetectionService.LaunchDetectionAlgorithm(imageRecordedEvent, _cancellationToken);
            if (detectionResult.HasError)
            {
                _logger.ProcessApplicationErrors(detectionResult.DomainErrors, eventId);
                return;
            }

            if (detectionResult.Value == null)
            {
                _logger.LogInformation(eventId, "No detection result");
                return;
            }

            DetectionEvent[] detectionEvents = detectionResult.Value;
            if(detectionEvents.Length == 0)
            {
                await _remoteStorageService.DeleteRemoteStorageFile(_commandData.RemoteStorageContainer, remoteStorageFilePath, _cancellationToken);
                _logger.LogInformation(eventId, $"No detection found deleting file from remote storage: {remoteStorageFilePath}");
                return;
            }


            foreach(DetectionEvent detectionEvent in detectionEvents)
            {
                var saveResult = await objectDetectionService.SaveDetectionToDb(detectionEvent,
                _commandData.RemoteStorageContainer, remoteStorageFilePath, _cancellationToken);
                if (saveResult.HasError)
                {
                    _logger.ProcessApplicationErrors(saveResult.DomainErrors, eventId);
                    return;
                }
                
                _logger.LogInformation(eventId, "Detection saved To DB");

                var pushResult = await objectDetectionService.PushDetectionToQueue(_commandData.DetectionQueue,
                    detectionEvent, _commandData.RemoteStorageContainer, remoteStorageFilePath,
                    _cancellationToken);
                if (pushResult.HasError)
                    _logger.ProcessApplicationErrors(pushResult.DomainErrors, eventId);

                _logger.LogInformation(eventId, $"Detection push to queue {_commandData.DetectionQueue}");
            }
            await unitOfWork.SaveAsync(_cancellationToken);
        }

        catch (Exception e)
        {
            _logger.LogError(eventId, e, "Unexpected Error");
            throw;
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