using Microsoft.Extensions.Logging;

namespace SecurityCamera.SharedKernel;

public interface ICommand<in TCommandData, TCommandResult>
{
    Task<TCommandResult> ProcessCommandAsync(TCommandData commandData, EventId eventId, CancellationToken cancellationToken);
}
