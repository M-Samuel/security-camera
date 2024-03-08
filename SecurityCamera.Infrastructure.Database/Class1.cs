using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.ImageRecorderDomain.Repository;

namespace SecurityCamera.Infrastructure.Database;

public class FakeRepository : IImageRecorderWriteRepository
{
    public Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}