using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityCamera.Domain.ObjectDetectionDomain;

namespace SecurityCamera.Infrastructure.Database.Entities;

public interface IObjectDetectionEntity
{
    DbSet<ImageDetection> ImageDetections { get; set; }
    void ImageDetectionBuilder(EntityTypeBuilder<ImageDetection> builder);
}