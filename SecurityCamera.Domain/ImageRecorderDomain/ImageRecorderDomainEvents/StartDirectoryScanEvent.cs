using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainEvents;

public record StartDirectoryScanEvent(DateTime OccurrenceDateTime, string Directory) : IDomainEvent;