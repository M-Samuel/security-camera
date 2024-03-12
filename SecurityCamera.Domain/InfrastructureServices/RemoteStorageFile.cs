namespace SecurityCamera.Domain.InfrastructureServices;

public class RemoteStorageFile
{
    public string ContainerName { get; set; }
    public string FilePath { get; set; }
    public bool FileDeleted { get; set; }
}