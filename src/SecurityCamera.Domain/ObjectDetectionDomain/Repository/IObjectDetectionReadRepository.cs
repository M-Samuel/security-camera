namespace SecurityCamera.Domain.ObjectDetectionDomain.Repository
{
    public interface IObjectDetectionReadRepository
    {
        Task<ImageDetection[]> GetAllDetectionByDate(DateOnly detectionDate, CancellationToken cancellationToken);
    }
}