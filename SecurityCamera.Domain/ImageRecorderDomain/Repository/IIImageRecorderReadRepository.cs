namespace SecurityCamera.Domain.ImageRecorderDomain.Repository
{
    public interface IImageRecorderReadRepository
    {
        Task GetAllDetectionByDate(DateOnly detectionDate, CancellationToken cancellationToken);
    }
}