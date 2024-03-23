using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ObjectDetectionDomain.Events;

public record DetectionEvent(
    DateTime OccurrenceDateTime, 
    string CameraName,
    string TempLocalImagePath,
    string ImageName, 
    DateTime ImageCreatedDateTime,
    DetectionType DetectionType
    ) : IDomainEvent;