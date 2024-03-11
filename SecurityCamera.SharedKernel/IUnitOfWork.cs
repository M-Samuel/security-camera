namespace SecurityCamera.SharedKernel;

public interface IUnitOfWork
{
    Task<int> SaveAsync(CancellationToken cancellationToken);
}