namespace SecurityCamera.SharedKernel;

public record InvalidOperationError(
    string Message,
    string ErrorName = nameof(ArgumentError)
) : IError;