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

    public ObjectDetectionRepository(TableClientsProvider tableClientsProvider)
    {
        _tableClient = tableClientsProvider.GetTableClientByKey($"{nameof(ImageDetection)}s");
    }


    public async Task<ImageDetection[]> GetAllDetectionByDate(DateOnly detectionDate, CancellationToken cancellationToken)
    {
        var queryTable = _tableClient.QueryAsync<ImageDetectionExt>(filter: i => DateOnly.FromDateTime(i.DetectionDateTime) == detectionDate, cancellationToken: cancellationToken);
        var array = await queryTable.AsAsyncEnumerable().ToArrayAsync();
        return array;
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken)
    {
        Response<IReadOnlyList<Response>> response = await _tableClient.SubmitTransactionAsync(transactionActions, cancellationToken);
        return response.Value.Count;
    }

    public Task SaveImageDetection(ImageDetection imageRecordedEvent, CancellationToken cancellationToken)
    {
        ImageDetectionExt imageDetectionExt = MapTo(imageRecordedEvent);
        transactionActions.Add(new TableTransactionAction(TableTransactionActionType.Add, imageDetectionExt));
        return Task.CompletedTask;
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
            Score = imageDetection.Score,
            OriginX = imageDetection.OriginX,
            OriginY = imageDetection.OriginY,
            Width = imageDetection.Width,
            Height = imageDetection.Height,
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
