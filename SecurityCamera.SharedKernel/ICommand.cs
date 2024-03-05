using Microsoft.Extensions.Logging;

namespace SecurityCamera.SharedKernel;

public interface ICommand<in TCommandData, TResult> where TCommandData:ICommandData<IDomainEvent>
{
    Task<TResult> ProcessCommandAsync(TCommandData commandData, EventId eventId, CancellationToken cancellationToken);
}
