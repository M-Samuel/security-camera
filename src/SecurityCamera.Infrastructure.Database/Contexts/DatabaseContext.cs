using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Infrastructure.Database.Entities;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Infrastructure.Database.Contexts;

public class DatabaseContext : DbContext, IObjectDetectionEntity, IUnitOfWork
{
    public DatabaseContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImageDetection>(ImageDetectionBuilder);
    }

    public DbSet<ImageDetection> ImageDetections { get; set; }

    public void ImageDetectionBuilder(EntityTypeBuilder<ImageDetection> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImageName)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(x => x.CameraName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(x => x.ImageSize)
            .IsRequired();
        builder.Property(x => x.DetectionData)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(x => x.DetectionDateTime)
            .IsRequired();
        builder.Property(x => x.RemoteStorageContainer)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(x => x.RemoteStorageFilePath)
            .HasMaxLength(2000)
            .IsRequired();
        builder.Property(x => x.Score);
        builder.Property(x => x.OriginX);
        builder.Property(x => x.OriginY);
        builder.Property(x => x.Width);
        builder.Property(x => x.Height);
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken)
    {
        return await SaveChangesAsync(cancellationToken);
    }
}