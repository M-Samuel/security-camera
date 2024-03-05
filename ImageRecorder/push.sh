#!/bin/bash
while true
do
    dotnet /app/ImageRecorder.dll --rabbitMqHostName=$RABBITMQ_HOSTNAME --queueName=$RABBITMQ_QUEUE_NAME --imagesDirPath=/images/resized --cameraName=$CAMERA_NAME --routingKey=$ROUTING_KEY
    echo "ImageRecorder crashed Respawning after 5s.."
    sleep 5
done