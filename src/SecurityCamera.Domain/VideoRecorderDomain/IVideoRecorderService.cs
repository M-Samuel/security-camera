using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.VideoRecorderDomain;

public interface IVideoRecorderService
{
    Task<Result<string[]>> GetVideoToBeUploaded(string videosDirectory);
    Task<Result<string[]>> RenameVideosWithUtc(string[] videoPaths);
    Task<Result<string[]>> UploadToRemoteStorage(string[] videoPaths, string remoteContainer, string remoteDirectory,
        bool deleteAfterUpload, CancellationToken cancellationToken);
}