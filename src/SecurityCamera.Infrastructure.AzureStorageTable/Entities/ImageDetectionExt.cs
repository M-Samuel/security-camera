using Azure;
using Azure.Data.Tables;
using SecurityCamera.Domain.ObjectDetectionDomain;

namespace SecurityCamera.Infrastructure.AzureStorageTable.Entities;


public class ImageDetectionExt : ImageDetection, ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; } = default;
    public ETag ETag { get; set; } = default;

}