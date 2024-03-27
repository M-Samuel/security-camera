namespace SecurityCamera.Domain.InfrastructureServices;

public class RemoteStorageContainer
{
    public required string ContainerName { get; set; }
    public bool IsDeleted { get; set; }
}