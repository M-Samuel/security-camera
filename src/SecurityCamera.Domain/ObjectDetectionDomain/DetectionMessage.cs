using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ObjectDetectionDomain;

public class DetectionMessage : QueueMessage
{
    public string? RemoteStorageContainer { get; set; }
    public string? RemoteStorageFilePath { get; set; }
    public string? CameraName { get; set; }
    public string? ImageName { get; set; }
    public DateTime ImageCreatedDateTime { get; set; }
    public required string DetectionData { get; set; }
    public double? Score { get; set; }
    public int? OriginX { get; set; }
    public int? OriginY { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}