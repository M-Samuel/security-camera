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
    --ModelName=$EnvModelName
    
    echo "Image Object Detection crashed Respawning after 5s.."
    sleep 5
done