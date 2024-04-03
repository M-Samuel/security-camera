using SecurityCamera.Application.Command.VideoRecorder;
using SecurityCamera.Domain.VideoRecorderDomain;

namespace SecurityCamera.Console.VideoRecorder;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly VideoRecorderCommand _videoRecorderCommand;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        VideoRecorderCommand videoRecorderCommand)
    {
        _logger = logger;
        _configuration = configuration;
        _videoRecorderCommand = videoRecorderCommand;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting...");
        
        VideoRecorderCommandData commandData = new()
        {
            RemoteStorageContainer = _configuration[nameof(Args.RemoteStorageContainer)] ?? "",
            RemoteStorageVideoDirectory = _configuration[nameof(Args.RemoteStorageVideoDirectory)] ?? "",
            LocalVideoDirectory = _configuration[nameof(Args.LocalVideoDirectory)] ?? "",
            DeleteAfterUpload = bool.Parse(_configuration[nameof(Args.DeleteAfterUpload)] ?? "false"),
        };

        while (true)
        {
            EventId eventId = new EventId(int.Parse(DateTime.UtcNow.ToString("MMddHHmmss")), Guid.NewGuid().ToString());
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                await _videoRecorderCommand.ProcessCommandAsync(commandData, eventId, stoppingToken);
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