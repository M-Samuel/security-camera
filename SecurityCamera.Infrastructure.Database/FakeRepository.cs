

using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;

namespace SecurityCamera.Infrastructure.Database;

public class FakeRepository : IObjectDetectionWriteRepository
{
    public Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}