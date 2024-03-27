using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.Events;

public record StartDirectoryScanEvent(DateTime OccurrenceDateTime, string Directory) : IDomainEvent;