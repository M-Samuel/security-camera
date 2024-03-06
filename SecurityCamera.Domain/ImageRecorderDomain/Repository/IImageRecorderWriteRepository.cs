namespace SecurityCamera.Domain.ImageRecorderDomain.Repository
{
    public interface IImageRecorderWriteRepository
    {
        Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken);
    }
}