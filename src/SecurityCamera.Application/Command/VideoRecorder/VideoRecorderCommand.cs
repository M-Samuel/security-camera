using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.VideoRecorderDomain;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Application.Command.VideoRecorder;

public class VideoRecorderCommand : ICommand<VideoRecorderCommandData, VideoRecorderCommandResult>
{
    private readonly IVideoRecorderService _videoRecorderService;
    private readonly ILogger<VideoRecorderCommand> _logger;

    public VideoRecorderCommand(
        IVideoRecorderService videoRecorderService,
        ILogger<VideoRecorderCommand> logger)
    {
        _videoRecorderService = videoRecorderService;
        _logger = logger;
    }
    public async Task<VideoRecorderCommandResult> ProcessCommandAsync(VideoRecorderCommandData commandData, EventId eventId, CancellationToken cancellationToken)
    {
        var rawVideoPathResult = await _videoRecorderService.GetVideoToBeUploaded(commandData.LocalVideoDirectory);
        if (rawVideoPathResult.HasError)
        {
            _logger.ProcessApplicationErrors(rawVideoPathResult.DomainErrors, eventId);
            return new VideoRecorderCommandResult();
        }

        string[] rawVideos = rawVideoPathResult.Value ?? [];
        _logger.LogInformation(eventId, $"{rawVideos.Length} detected");
        if (rawVideos.Length == 0)
            return new VideoRecorderCommandResult();
        
        
        Array.ForEach(rawVideos, (videoPath) => _logger.LogInformation(eventId, $"Video detected: {videoPath}"));
        
        var renameVideosResult = await _videoRecorderService.RenameVideosWithUtc(rawVideoPathResult.Value ?? []);
        if (renameVideosResult.HasError)
        {
            _logger.ProcessApplicationErrors(renameVideosResult.DomainErrors, eventId);
            return new VideoRecorderCommandResult();
        }
        
        string[] renamedVideos = renameVideosResult.Value ?? [];
        _logger.LogInformation(eventId, $"{renamedVideos.Length} renamed");
        Array.ForEach(renamedVideos, (videoPath) => _logger.LogInformation(eventId, $"Video renamed to: {videoPath}"));

        var uploadVideoResult = await _videoRecorderService.UploadToRemoteStorage(
            renamedVideos,
            commandData.RemoteStorageContainer,
            commandData.RemoteStorageVideoDirectory,
            commandData.DeleteAfterUpload,
            cancellationToken
        );
        
        if (rawVideoPathResult.HasError)
            _logger.ProcessApplicationErrors(rawVideoPathResult.DomainErrors, eventId);
        
        string[] uploadedVideos = uploadVideoResult.Value ?? [];
        _logger.LogInformation(eventId, $"{uploadedVideos.Length} uploaded");
        Array.ForEach(uploadedVideos, (videoPath) => _logger.LogInformation(eventId, $"Container {commandData.RemoteStorageContainer}, Video uploaded to: {videoPath}"));
        
        return new VideoRecorderCommandResult();
    }
}

public class VideoRecorderCommandResult
{
}

public class VideoRecorderCommandData
{
    public required string LocalVideoDirectory { get; set; }
    public required string RemoteStorageContainer { get; set; }
    public required string RemoteStorageVideoDirectory { get; set; }
    public bool DeleteAfterUpload { get; set; }
}