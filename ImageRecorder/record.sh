#!/bin/bash
while true
do
    ffmpeg -re -rtsp_transport tcp -i $RTSP -vf "fps=$FPS" "/images/image_%04d.png" && echo "Image Generated to /images"
    echo "ImageRecorder crashed Respawning after 5s.."
    sleep 5
done