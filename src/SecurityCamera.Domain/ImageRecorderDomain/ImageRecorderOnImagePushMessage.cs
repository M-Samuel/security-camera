using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain;

public class ImageRecorderOnImagePushMessage : QueueMessage
{
    public required string RemoteStorageContainer { get; set; }
    public required string RemoteStorageFilePath { get; set; }
    public required string CameraName { get; set; }
    public required string ImageName { get; set; }
    public DateTime ImageCreatedDateTime { get; set; }
}