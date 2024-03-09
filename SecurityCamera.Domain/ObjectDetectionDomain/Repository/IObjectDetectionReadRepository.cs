namespace SecurityCamera.Domain.ObjectDetectionDomain.Repository
{
    public interface IObjectDetectionReadRepository
    {
        Task GetAllDetectionByDate(DateOnly detectionDate, CancellationToken cancellationToken);
    }
}