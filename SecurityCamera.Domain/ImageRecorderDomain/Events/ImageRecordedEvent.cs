using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.Events;

public record ImageRecordedEvent(
    DateTime OccurrenceDateTime, 
    string CameraName,
    string TempLocalImagePath, 
    string ImageName, 
    DateTime ImageCreatedDateTime
    ) : IDomainEvent;