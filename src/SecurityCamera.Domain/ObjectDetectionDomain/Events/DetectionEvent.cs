using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ObjectDetectionDomain.Events;

public record DetectionEvent(
    DateTime OccurrenceDateTime, 
    string CameraName,
    byte[] ImageBytes,
    string ImageName, 
    DateTime ImageCreatedDateTime,
    string DetectionData
    ) : IDomainEvent;