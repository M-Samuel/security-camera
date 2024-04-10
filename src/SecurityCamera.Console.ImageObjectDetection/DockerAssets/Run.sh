#!/bin/bash
while true
do
    /app/SecurityCamera.Console.ImageObjectDetection \
    --AzureServiceBusConnectionString=$EnvAzureServiceBusConnectionString \
    --AzureStorageConnectionString=$EnvAzureStorageConnectionString \
    --RemoteStorageContainer=$EnvRemoteStorageContainer \
    --RemoteStorageFileDirectory=$EnvRemoteStorageFileDirectory \
    --ServiceBusQueueImageRecords=$EnvServiceBusQueueImageRecords \
    --ServiceBusQueueDetections=$EnvServiceBusQueueDetections \ 
    --UltralyticsAiModelName=$EnvUltralyticsAiModelName
    
    echo "Image Object Detection crashed Respawning after 5s.."
    sleep 5
done