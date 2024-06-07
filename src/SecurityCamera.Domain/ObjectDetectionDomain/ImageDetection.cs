using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ObjectDetectionDomain;

public class ImageDetection : IEntity
{
    public Guid Id { get; set; }
    public required string CameraName { get; set; }
    public required string ImageName { get; set; }
    public required int ImageSize { get; set; }
    public required string DetectionData { get; set; }
    public required DateTime DetectionDateTime { get; set; }
    public required string RemoteStorageContainer { get; set; }
    public required string RemoteStorageFilePath { get; set; }
    public double? Score { get; set; }
    public int? OriginX { get; set; }
    public int? OriginY { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}