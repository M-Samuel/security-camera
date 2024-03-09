using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.Events;

public record ImageRecordedEvent(
    DateTime OccurrenceDateTime, 
    string CameraName,
    byte[] ImageBytes, 
    string ImageName, 
    DateTime ImageCreatedDateTime
    ) : IDomainEvent;