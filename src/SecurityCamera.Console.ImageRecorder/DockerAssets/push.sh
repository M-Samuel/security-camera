#!/bin/bash
while true
do
    dotnet /app/SecurityCamera.Console.ImageRecorder.dll \
    --CameraName=$EnvCameraName \
    --ServiceBusQueueImageRecords=$EnvServiceBusQueueImageRecords \
    --ImagesDirPath=$EnvImagesDirPath \
    --RemoteStorageContainer=$EnvRemoteStorageContainer \
    --RemoteStorageFileDirectory=$EnvRemoteStorageFileDirectory \
    --AzureServiceBusConnectionString=$EnvAzureServiceBusConnectionString \
    --AzureStorageConnectionString=$EnvAzureStorageConnectionString 
    
    echo "ImageRecorder crashed Respawning after 5s.."
    sleep 5
done