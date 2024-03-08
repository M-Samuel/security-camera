#!/bin/bash
while true
do
    dotnet /app/SecurityCamera.Console.ImageRecorder.dll --RabbitMqHostName=$RABBITMQ_HOSTNAME --QueueName=$RABBITMQ_QUEUE_NAME --ImagesDirPath=/images/resized --CameraName=$CAMERA_NAME --RoutingKey=$ROUTING_KEY
    echo "ImageRecorder crashed Respawning after 5s.."
    sleep 5
done