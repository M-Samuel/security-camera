using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Nelibur.ObjectMapper;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;
using SecurityCamera.Infrastructure.AzureStorageTable.Entities;

namespace SecurityCamera.Infrastructure.AzureStorageTable;

public class ObjectDetectionRepository : IObjectDetectionReadRepository, IObjectDetectionWriteRepository
{
    private readonly TableClient _tableClient;
    private bool isTableCreated = false;

    public ObjectDetectionRepository(IConfiguration configuration)
    {
        TableServiceClient tableServiceClient = new TableServiceClient(configuration[nameof(EnvVars.AzureTableStorageConnectionString)]);
        _tableClient = tableServiceClient.GetTableClient(
            tableName: $"{nameof(ImageDetection)}s"
        );

        TinyMapper.Bind<ImageDetection, ImageDetectionExt>(config => {
            config.Bind(source => source.Id, target => target.RowKey);
            config.Bind(source => source.Id, target => target.Id);
            config.Bind(source => source.CameraName, target => target.PartitionKey);
            config.Bind(source => source.CameraName, target => target.CameraName);
        });
    }


    public async Task<ImageDetection[]> GetAllDetectionByDate(DateOnly detectionDate, CancellationToken cancellationToken)
    {
        await CreateTableIfNotExists(cancellationToken);

        throw new NotImplementedException();
    }

    public async Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken)
    {
        await CreateTableIfNotExists(cancellationToken);
        ImageDetectionExt imageDetectionExt = TinyMapper.Map<ImageDetectionExt>(imageRecordedEvent);
        await _tableClient.AddEntityAsync(imageDetectionExt);
    }

    private async Task CreateTableIfNotExists(CancellationToken cancellationToken)
    {
        if (!isTableCreated)
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
            isTableCreated = true;
        }
    }
}
