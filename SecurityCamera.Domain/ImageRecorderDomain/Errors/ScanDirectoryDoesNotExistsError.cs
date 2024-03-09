using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.Errors;

public record ScanDirectoryDoesNotExistsError(
    string Message,
    string ErrorName = nameof(ScanDirectoryDoesNotExistsError)
    ) : IError;