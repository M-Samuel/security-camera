using SecurityCamera.Application.Command.ObjectDetection;
using SecurityCamera.Domain.ObjectDetectionDomain;

namespace SecurityCamera.Console.ImageObjectDetection;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
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

        while (true)
        {
            ObjectDetectionCommand? objectDetectionCommand = _serviceProvider.GetService<ObjectDetectionCommand>();
            if (objectDetectionCommand == null)
                throw new InvalidOperationException($"{nameof(ObjectDetectionCommand)} service not registered");
            
            EventId eventId = new EventId(new Random().Next(), Guid.NewGuid().ToString());
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                await objectDetectionCommand.ProcessCommandAsync(commandData, eventId, stoppingToken);
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