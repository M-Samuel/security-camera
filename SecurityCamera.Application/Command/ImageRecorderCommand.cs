

namespace SecurityCamera.Application.Usecases;

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
        var imageRecorderEvent = new ImageRecorderEvent(commandData.ImageData);
        await _imageRecorderService.RecordImageAsync(imageRecorderEvent, cancellationToken);
        return new ImageRecorderCommandResult();
    }
}
