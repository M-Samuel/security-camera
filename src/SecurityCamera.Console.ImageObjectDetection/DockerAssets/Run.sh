#!/bin/bash
while true
do
    dotnet /app/SecurityCamera.Console.ImageObjectDetection.dll \
    --AzureServiceBusConnectionString=$EnvAzureServiceBusConnectionString \
    --AzureStorageConnectionString=$EnvAzureStorageConnectionString \
    --RemoteStorageContainer=$EnvRemoteStorageContainer \
    --RemoteStorageFileDirectory=$EnvRemoteStorageFileDirectory \
    --ServiceBusQueueImageRecords=$EnvServiceBusQueueImageRecords \
    --ServiceBusQueueDetections=$EnvServiceBusQueueDetections
    
    echo "Image Object Detection crashed Respawning after 5s.."
    sleep 5
done