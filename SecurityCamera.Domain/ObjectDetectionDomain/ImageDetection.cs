using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ObjectDetectionDomain;

public class ImageDetection : IEntity
{
    public Guid Id { get; set; }
    public required string CameraName { get; set; }
    public required string ImageName { get; set; }
    public required int ImageSize { get; set; }
    public required DetectionType DetectionType { get; set; }
    public required DateTime DetectionDateTime { get; set; }
}