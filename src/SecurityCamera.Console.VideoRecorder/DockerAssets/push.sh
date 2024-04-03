#!/bin/bash
while true
do
    dotnet /app/SecurityCamera.Console.VideoRecorder.dll \
    --AzureStorageConnectionString=$EnvAzureStorageConnectionString \
    --RemoteStorageContainer=$EnvRemoteStorageContainer \
    --RemoteStorageVideoDirectory=$EnvRemoteStorageVideoDirectory \
    --LocalVideoDirectory=$EnvLocalVideoDirectory \
    --DeleteAfterUpload=$EnvDeleteAfterUpload 
    
    echo "VideoRecorder crashed Respawning after 5s.."
    sleep 5
done