using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Application.Command.ObjectDetection;

public class ObjectDetectionCommand : ICommand<ObjectDetectionCommandData, ObjectDetectionCommandResult>
{
    private readonly ILogger<ObjectDetectionCommand> _logger;
    private readonly IObjectDetectionService _objectDetectionService;
    private readonly IQueueConsumerService _queueConsumerService;

    public ObjectDetectionCommand(
        ILogger<ObjectDetectionCommand> logger,
        IObjectDetectionService objectDetectionService,
        IQueueConsumerService queueConsumerService
    )
    {
        _logger = logger;
        _objectDetectionService = objectDetectionService;
        _queueConsumerService = queueConsumerService;
    }

    public async Task<ObjectDetectionCommandResult> ProcessCommandAsync(ObjectDetectionCommandData commandData, EventId eventId, CancellationToken cancellationToken)
    {
        await _queueConsumerService.GetMessageFromQueue(
            commandData.ImageQueue,
            async (queueMessage) => {
                ImageRecordedEvent imageRecordedEvent = new(
                    OccurrenceDateTime: DateTime.Now,
                    CameraName: queueMessage.QueueMessageHeaders?.FirstOrDefault(x => x.Key == nameof(ImageRecordedEvent.CameraName))?.Value ?? "" ,
                    ImageBytes: queueMessage.Body,
                    ImageCreatedDateTime: DateTime.Parse(queueMessage.QueueMessageHeaders?.FirstOrDefault(x => x.Key == nameof(ImageRecordedEvent.ImageCreatedDateTime))?.Value ?? ""),
                    ImageName: queueMessage.QueueMessageHeaders?.FirstOrDefault(x => x.Key == nameof(ImageRecordedEvent.ImageName))?.Value ?? ""
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

                var saveResult = await _objectDetectionService.SaveDetectionToDb(detectionResult.Value, cancellationToken);
                if(saveResult.HasError)
                    _logger.ProcessApplicationErrors(saveResult.DomainErrors, eventId);
                else
                {
                    var pushResult = await _objectDetectionService.PushDetectionToQueue(commandData.DetectionQueue, detectionResult.Value, cancellationToken);
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
}