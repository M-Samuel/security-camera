namespace SecurityCamera.SharedKernel;

public interface IError
{
    string Message { get; }
    string ErrorName { get; }
}