namespace SecurityCamera.SharedKernel;

public record ArgumentError(
    string Message,
    string ErrorName = nameof(ArgumentError)
) : IError;