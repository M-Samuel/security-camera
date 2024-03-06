using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;

public record DetectionEvent(
    DateTime OccurrenceDateTime, 
    string CameraName,
    byte[] ImageBytes,
    string ImageName, 
    DateTime ImageCreatedDateTime,
    DetectionType DetectionType
    ) : IDomainEvent;