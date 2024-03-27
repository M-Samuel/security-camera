namespace SecurityCamera.Domain.ImageRecorderDomain;

public enum Args
{
    RabbitMqHostName,
    ServiceBusQueueImageRecords,
    ImagesDirPath,
    CameraName,
    RoutingKey,
    RemoteStorageContainer,
    RemoteStorageFileDirectory
}