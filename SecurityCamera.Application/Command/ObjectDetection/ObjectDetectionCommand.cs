using Microsoft.Extensions.DependencyInjection;
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
    private readonly IQueueConsumerService<ImageRecorderOnImagePushMessage> _queueConsumerService;
    private readonly IRemoteStorageService _remoteStorageService;
    private ObjectDetectionCommandData? _commandData;
    private EventId _eventId;
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
        EventId eventId = new EventId((int)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds, Guid.NewGuid().ToString());
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            IObjectDetectionService objectDetectionService = scope.ServiceProvider.GetRequiredService<IObjectDetectionService>();
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
                CameraName: queueMessage.CameraName ?? "",
                ImageBytes: remoteStorageFile.FileContent,
                ImageCreatedDateTime: queueMessage.ImageCreatedDateTime,
                ImageName: queueMessage.ImageName ?? ""
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

            var saveResult = await objectDetectionService.SaveDetectionToDb(detectionResult.Value,
                _commandData.RemoteStorageContainer, remoteStorageFilePath, _cancellationToken);
            if (saveResult.HasError)
            {
                _logger.ProcessApplicationErrors(saveResult.DomainErrors, eventId);
                return;
            }

            await unitOfWork.SaveAsync(_cancellationToken);
            _logger.LogInformation(eventId, "Detection saved To DB");
            
            var pushResult = await objectDetectionService.PushDetectionToQueue(_commandData.DetectionQueue,
                detectionResult.Value, _commandData.RemoteStorageContainer, remoteStorageFilePath,
                _cancellationToken);
            if (pushResult.HasError)
                _logger.ProcessApplicationErrors(pushResult.DomainErrors, eventId);
            
            _logger.LogInformation(eventId, $"Detection push to queue {_commandData.DetectionQueue}");
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