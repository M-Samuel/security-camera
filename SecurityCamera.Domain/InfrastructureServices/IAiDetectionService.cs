using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;

namespace SecurityCamera.Domain.InfrastructureServices;

public interface IAiDetectionService 
{
    IEnumerable<DetectionEvent> AnalyseImage(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken);
}