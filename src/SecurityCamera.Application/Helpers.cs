using Microsoft.Extensions.Logging;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Application;

public static class Helpers
{
    public static void ProcessApplicationErrors(this ILogger logger, IEnumerable<IError> errors, EventId eventId)
    {
        foreach (IError error in errors)
            logger.LogError(eventId, $"Type: {error.ErrorName}, Name: {error.Message}");

        try
        {
            throw new InvalidOperationException("Command contain domain errors, cannot continue");
        }
        catch (InvalidOperationException ioe)
        {
            logger.LogError(eventId, ioe,"Cannot continue, exception raised");
            throw;
        }
    }
}