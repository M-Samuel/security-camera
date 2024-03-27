using SecurityCamera.Application.Command.ImageRecorder;
using SecurityCamera.Domain.ImageRecorderDomain;

namespace SecurityCamera.Console.ImageRecorder;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly ImageRecorderCommand _imageRecorderCommand;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, ImageRecorderCommand imageRecorderCommand)
    {
        _logger = logger;
        _configuration = configuration;
        _imageRecorderCommand = imageRecorderCommand;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting...");
        
        ImageRecorderCommandData commandData = new()
        {
            CameraName = _configuration[nameof(Args.CameraName)] ?? "",
            ImageDirectory = _configuration[nameof(Args.ImagesDirPath)] ?? "",
            QueueName = _configuration[nameof(Args.ServiceBusQueueImageRecords)] ?? "",
            RemoteStorageContainer = _configuration[nameof(Args.RemoteStorageContainer)] ?? "",
            RemoteStorageFileDirectory = _configuration[nameof(Args.RemoteStorageFileDirectory)] ?? "",
        };

        while (true)
        {
            EventId eventId = new EventId(new Random().Next(), Guid.NewGuid().ToString());
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                await _imageRecorderCommand.ProcessCommandAsync(commandData, eventId, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(eventId, e, "Fatal error...");
                throw;
            }

            await Task.Delay(3000, stoppingToken);
        }
    }

}
