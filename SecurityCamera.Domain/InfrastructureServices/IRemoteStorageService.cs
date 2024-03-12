namespace SecurityCamera.Domain.InfrastructureServices;

public interface IRemoteStorageService
{
    Task<RemoteStorageContainer> CreateRemoteStorageContainer(string containerName, CancellationToken cancellationToken);
    Task<RemoteStorageContainer> DeleteRemoteStorageContainer(string containerName, CancellationToken cancellationToken);
    Task<RemoteStorageFile> UploadRemoteStorageFile(string containerName, string filePath, byte[] fileBytes, CancellationToken cancellationToken);
    Task<RemoteStorageFile> UploadRemoteStorageFile(string containerName, string filePath, Stream stream, CancellationToken cancellationToken);
    Task<RemoteStorageFile> UploadRemoteStorageLargeFile(string containerName, string filePath, Stream stream, CancellationToken cancellationToken);
    Task<RemoteStorageFile> DeleteRemoteStorageFile(string containerName, string filePath, CancellationToken cancellationToken);
    IAsyncEnumerable<RemoteStorageFile> ListRemoteStorageFiles(string containerName, CancellationToken cancellationToken);
}