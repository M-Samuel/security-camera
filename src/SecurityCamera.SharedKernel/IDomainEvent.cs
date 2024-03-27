namespace SecurityCamera.SharedKernel;

public interface IDomainEvent
{
    DateTime OccurrenceDateTime { get; }
}