using SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;

namespace SecurityCamera.Domain.InfrastructureServices;

public interface IAIDectectionService 
{
    Task<DetectionEvent?> AnalyseImage(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken);
}