using SecurityCamera.Domain.InfrastructureServices;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public class ImageRecorderOnImagePushMessage : QueueMessage
{
    public string? RemoteStorageContainer { get; set; }
    public string? RemoteStorageFilePath { get; set; }
    public string? CameraName { get; set; }
    public string? ImageName { get; set; }
    public DateTime ImageCreatedDateTime { get; set; }
}