using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain.Errors;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.VideoRecorderDomain;

public class VideoRecorderService : IVideoRecorderService
{
    private readonly IRemoteStorageService _remoteStorageService;
    private readonly ILogger<VideoRecorderService> _logger;

    public VideoRecorderService(
    IRemoteStorageService remoteStorageService,
    ILogger<VideoRecorderService> logger
    )
    {
        _remoteStorageService = remoteStorageService;
        _logger = logger;
    }
    
    public async Task<Result<string[]>> GetVideoToBeUploaded(string videosDirectory)
    {
        Result<string[]> result = new Result<string[]>(null);
        result.AddErrorIf(() => string.IsNullOrWhiteSpace(videosDirectory), new ArgumentError(videosDirectory));
        result.AddErrorIf(() => !Directory.Exists(videosDirectory), new ScanDirectoryDoesNotExistsError(videosDirectory));
        
        List<string> videoPaths = new List<string>();
        foreach (string videoPath in  Directory.EnumerateFiles(videosDirectory, "*.mp4"))
        {
            FileInfo fileInfo = new FileInfo(videoPath);
            if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc <= TimeSpan.FromSeconds(10))
                continue;

            if (fileInfo.Length == 0)
            {
                fileInfo.Delete();
                continue;
            }
            
            videoPaths.Add(videoPath);
            
        }
        
        result.UpdateValueIfNoError(videoPaths.ToArray());
        return await Task.FromResult(result);
    }

    public async Task<Result<string[]>> RenameVideosWithUtc(string[] videoPaths)
    {
        
        List<string> newVideoPaths = new List<string>();
        foreach (string videoPath in videoPaths)
        {
            FileInfo fileInfo = new FileInfo(videoPath);
            string newVideoName = $"{fileInfo.CreationTimeUtc:yyyyMMddHHmmss}_video.mp4";

            string videoDirectory = Path.GetDirectoryName(videoPath) ?? string.Empty;
            if(string.IsNullOrWhiteSpace(videoDirectory))
                continue;

            string newVideoPath = Path.Combine(videoDirectory, newVideoName);
            File.Move(videoPath, newVideoPath);
            
            newVideoPaths.Add(newVideoPath);
        }
        
        Result<string[]> result = new Result<string[]>(newVideoPaths.ToArray());
        return await Task.FromResult(result);
    }

    public async Task<Result<string[]>> UploadToRemoteStorage(string[] videoPaths, string remoteContainer, string remoteDirectory, bool deleteAfterUpload, CancellationToken cancellationToken)
    {
        Result<string[]> result = new Result<string[]>(null);
        List<string> remoteVideoPaths = new List<string>();

        foreach (string videoPath in videoPaths)
        {
            try
            {
                _logger.LogInformation($"Stating Video {videoPath} uploading...");
                string remoteVideoPath = Path.Combine(remoteDirectory, Path.GetFileName(videoPath));
                await _remoteStorageService.UploadRemoteStorageLargeFile(remoteContainer, remoteVideoPath,
                    File.OpenRead(videoPath), cancellationToken);
                remoteVideoPaths.Add(remoteVideoPath);
                _logger.LogInformation($"Video {videoPath} uploaded to {remoteVideoPath}");

                if (deleteAfterUpload)
                {
                    _logger.LogInformation($"Video {videoPath} deleted");
                    File.Delete(videoPath);
                }
                    
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unable to upload {videoPath}");
                result.AddErrorIf(() => true, new InvalidOperationError($"Unable to upload {videoPath}"));
            }
        }
        
        result.UpdateValueIfNoError(remoteVideoPaths.ToArray());

        return await Task.FromResult(result);
    }
}