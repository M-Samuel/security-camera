using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;
using SecurityCamera.Infrastructure.AzureStorageTable.Entities;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Infrastructure.AzureStorageTable;

public class ObjectDetectionRepository : IObjectDetectionReadRepository, IObjectDetectionWriteRepository, IUnitOfWork
{
    private List<TableTransactionAction> transactionActions = new List<TableTransactionAction>();
    private readonly TableClient _tableClient;
    private bool isTableCreated = false;

    public ObjectDetectionRepository(IConfiguration configuration)
    {
        TableServiceClient tableServiceClient = new TableServiceClient(configuration[nameof(EnvVars.AzureTableStorageConnectionString)]);
        _tableClient = tableServiceClient.GetTableClient(
            tableName: $"{nameof(ImageDetection)}s"
        );

    }


    public async Task<ImageDetection[]> GetAllDetectionByDate(DateOnly detectionDate, CancellationToken cancellationToken)
    {
        await CreateTableIfNotExists(cancellationToken);
        var queryTable = _tableClient.QueryAsync<ImageDetectionExt>(filter: i => DateOnly.FromDateTime(i.DetectionDateTime) == detectionDate, cancellationToken: cancellationToken);
        var array = await queryTable.AsAsyncEnumerable().ToArrayAsync();
        return array;
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken)
    {
        Response<IReadOnlyList<Response>> response = await _tableClient.SubmitTransactionAsync(transactionActions, cancellationToken);
        return response.Value.Count;
    }

    public async Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken)
    {
        await CreateTableIfNotExists(cancellationToken);
        ImageDetectionExt imageDetectionExt = MapTo(imageRecordedEvent);
        transactionActions.Add(new TableTransactionAction(TableTransactionActionType.Add, imageDetectionExt));
    }

    private async Task CreateTableIfNotExists(CancellationToken cancellationToken)
    {
        if (!isTableCreated)
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);
            isTableCreated = true;
        }
    }

    private static ImageDetectionExt MapTo(ImageDetection imageDetection)
    {
        return new ImageDetectionExt(){
            CameraName = imageDetection.CameraName,
            DetectionData = imageDetection.DetectionData,
            DetectionDateTime = imageDetection.DetectionDateTime,
            ImageName = imageDetection.ImageName,
            ImageSize = imageDetection.ImageSize,
            RemoteStorageContainer = imageDetection.RemoteStorageContainer,
            RemoteStorageFilePath = imageDetection.RemoteStorageFilePath,
            Id = imageDetection.Id,
            RowKey = imageDetection.Id.ToString(),
            PartitionKey = imageDetection.CameraName
        };
    }

    private static ImageDetection MapTo(ImageDetectionExt imageDetectionExt)
    {
        return new ImageDetection(){
            CameraName = imageDetectionExt.CameraName,
            DetectionData = imageDetectionExt.DetectionData,
            DetectionDateTime = imageDetectionExt.DetectionDateTime,
            ImageName = imageDetectionExt.ImageName,
            ImageSize = imageDetectionExt.ImageSize,
            RemoteStorageContainer = imageDetectionExt.RemoteStorageContainer,
            RemoteStorageFilePath = imageDetectionExt.RemoteStorageFilePath,
            Id = imageDetectionExt.Id,
        };
    }
}
