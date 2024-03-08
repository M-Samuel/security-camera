using SecurityCamera.SharedKernel;

namespace SecurityCamera.Application.Command.ImageRecorder;

public class ImageRecorderCommandData
{
    public required string ImageDirectory { get; set; }
    public required string CameraName { get; set; }
    public required string QueueName { get; set; }
}