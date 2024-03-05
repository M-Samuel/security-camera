using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;

public record ImageDetectedEvent(
    DateTime OccurrenceDateTime, 
    string CameraName,
    byte[] ImageBytes, 
    string ImageName, 
    DateTime ImageCreatedDateTime
    ) : IDomainEvent;