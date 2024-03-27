namespace SecurityCamera.Domain.ObjectDetectionDomain.Repository
{
    public interface IObjectDetectionWriteRepository
    {
        Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken);
    }
}