namespace SecurityCamera.SharedKernel;

public interface ICommandData<out TEvent> where TEvent : IDomainEvent
{
    TEvent ToEvent();
}