

using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Application.Command.ImageRecorder;

public class ImageRecorderCommand : ICommand<ImageRecorderCommandData, ImageRecorderCommandResult>
{
    private readonly ILogger<ImageRecorderCommand> _logger;
    private readonly IImageRecorderService _imageRecorderService;

    public ImageRecorderCommand(
        ILogger<ImageRecorderCommand> logger,
        IImageRecorderService imageRecorderService)
    {
        _logger = logger;
        _imageRecorderService = imageRecorderService;
    }

    public async Task<ImageRecorderCommandResult> ProcessCommandAsync(ImageRecorderCommandData commandData, EventId eventId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(eventId, "Processing Image Recorder Command");
        StartDirectoryScanEvent startDirectoryScanEvent = new(DateTime.UtcNow, commandData.ImageDirectory);
        Result<ImageRecordedEvent[]> scanResult = await _imageRecorderService.ScanDirectory(startDirectoryScanEvent, commandData.CameraName, cancellationToken);
        if(scanResult.HasError)
            _logger.ProcessApplicationErrors(scanResult.DomainErrors, eventId);

        ImageRecordedEvent[]? imageRecordEvents = scanResult.Value;
        if(imageRecordEvents == null)
            return new ImageRecorderCommandResult()
            {
                RecordedImagesCount = 0
            };

        
        foreach (ImageRecordedEvent imageRecordedEvent in imageRecordEvents)
        {
            string newImageName = $"{imageRecordedEvent.ImageCreatedDateTime:yyyyMMddHHmmss}_image.png";
            string remoteStorageFilePath = $"{commandData.RemoteStorageFileDirectory}/{newImageName}";
            var saveToRemoteResult = await _imageRecorderService.SaveImageToRemoteStorage(imageRecordedEvent, commandData.RemoteStorageContainer, remoteStorageFilePath, cancellationToken);
            if(saveToRemoteResult.HasError){
                _logger.ProcessApplicationErrors(saveToRemoteResult.DomainErrors, eventId);
                continue;
            }
            var pushResult = await _imageRecorderService.PushImagePathToQueue(imageRecordedEvent, commandData.QueueName, commandData.RemoteStorageContainer, remoteStorageFilePath, cancellationToken);
            if(pushResult.HasError){
                _logger.ProcessApplicationErrors(pushResult.DomainErrors, eventId);
                continue;
            }

            var queueMessage = pushResult.Value;
            if(queueMessage != null)
                _logger.LogInformation(eventId,$"Message to queue {commandData.QueueName}, {imageRecordedEvent.ImageName}");
        }
        
        
        return new ImageRecorderCommandResult()
        {
            RecordedImagesCount = imageRecordEvents.Length
        };
    }
}

