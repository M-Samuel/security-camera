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
        ValidateArgs();
        EventId eventId = new EventId(0, Guid.NewGuid().ToString());

        ImageRecorderCommandData commandData = new()
        {
            CameraName = _configuration[nameof(Args.CameraName)] ?? "",
            ImageDirectory = _configuration[nameof(Args.ImagesDirPath)] ?? "",
            QueueName = _configuration[nameof(Args.QueueName)] ?? ""
        };
        await _imageRecorderCommand.ProcessCommandAsync(commandData, eventId, stoppingToken);
        
    }

    private void ValidateArgs()
    {
        if(string.IsNullOrWhiteSpace(nameof(Args.CameraName)))
            throw new ArgumentNullException(nameof(Args.CameraName));
        if(string.IsNullOrWhiteSpace(nameof(Args.QueueName)))
            throw new ArgumentNullException(nameof(Args.QueueName));
        if(string.IsNullOrWhiteSpace(nameof(Args.RoutingKey)))
            throw new ArgumentNullException(nameof(Args.RoutingKey));
        if(string.IsNullOrWhiteSpace(nameof(Args.ImagesDirPath)))
            throw new ArgumentNullException(nameof(Args.ImagesDirPath));
        if(string.IsNullOrWhiteSpace(nameof(Args.RabbitMqHostName)))
            throw new ArgumentNullException(nameof(Args.RabbitMqHostName));
    }
}
