

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
    private readonly IAiDetectionService _detectionService;

    public ImageRecorderCommand(
        ILogger<ImageRecorderCommand> logger,
        IImageRecorderService imageRecorderService,
        IAiDetectionService detectionService)
    {
        _logger = logger;
        _detectionService = detectionService;
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
            var dtec = await _detectionService.AnalyseImage(imageRecordedEvent, cancellationToken).FirstOrDefaultAsync(cancellationToken);
            var pushResult = await _imageRecorderService.PushImageToQueue(imageRecordedEvent, commandData.QueueName, cancellationToken);
            if(pushResult.HasError)
                _logger.ProcessApplicationErrors(pushResult.DomainErrors, eventId);

            var queueMessage = pushResult.Value;
            if(queueMessage != null)
                _logger.LogInformation(eventId,$"Message to queue {commandData.QueueName}, {queueMessage.Body.Length}");
        }
        
        
        return new ImageRecorderCommandResult()
        {
            RecordedImagesCount = imageRecordEvents.Length
        };
    }
}

