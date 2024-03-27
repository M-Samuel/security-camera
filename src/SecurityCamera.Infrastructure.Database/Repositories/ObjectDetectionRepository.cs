
using Microsoft.EntityFrameworkCore;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;
using SecurityCamera.Infrastructure.Database.Contexts;

namespace SecurityCamera.Infrastructure.Database.Repositories;
public class ObjectDetectionRepository : IObjectDetectionWriteRepository, IObjectDetectionReadRepository
{
    private readonly DatabaseContext _databaseContext;

    public ObjectDetectionRepository(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task<ImageDetection[]> GetAllDetectionByDate(DateOnly detectionDate, CancellationToken cancellationToken)
    {
        return await _databaseContext.ImageDetections
            .Where(x => DateOnly.FromDateTime(x.DetectionDateTime) == detectionDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken)
    {
        _databaseContext.Update(imageRecordedEvent);
        await Task.CompletedTask;
    }
}