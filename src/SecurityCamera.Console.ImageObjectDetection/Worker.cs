using SecurityCamera.Application.Command.ObjectDetection;
using SecurityCamera.Domain.ObjectDetectionDomain;

namespace SecurityCamera.Console.ImageObjectDetection;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly ObjectDetectionCommand _objectDetectionCommand;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, ObjectDetectionCommand objectDetectionCommand, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _objectDetectionCommand = objectDetectionCommand;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting...");
        
        ObjectDetectionCommandData commandData = new()
        {
            RemoteStorageContainer = _configuration[nameof(Args.RemoteStorageContainer)] ?? string.Empty,
            RemoteStorageFileDirectory = _configuration[nameof(Args.RemoteStorageFileDirectory)] ?? string.Empty,
            ImageQueue = _configuration[nameof(Args.ServiceBusQueueImageRecords)] ?? string.Empty,
            DetectionQueue = _configuration[nameof(Args.ServiceBusQueueDetections)] ?? string.Empty,
        };
        
        EventId eventId = new EventId((int)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds, Guid.NewGuid().ToString());
        stoppingToken.ThrowIfCancellationRequested();
        await _objectDetectionCommand.ProcessCommandAsync(commandData, eventId, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            //Waiting for cancellation
            await Task.Delay(5000, stoppingToken);
        }
    }
}