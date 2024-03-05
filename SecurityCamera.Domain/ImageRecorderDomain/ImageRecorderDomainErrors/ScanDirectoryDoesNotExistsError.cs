using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.ImageRecorderDomain.ImageRecorderDomainErrors;

public record ScanDirectoryDoesNotExistsError(
    string Message,
    string ErrorName = nameof(ScanDirectoryDoesNotExistsError)
    ) : IError;