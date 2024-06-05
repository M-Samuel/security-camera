#!/bin/bash
while true
do
    ffmpeg -re -rtsp_transport tcp -i $RTSP -vf "fps=$FPS" -qscale:v 2 "$EnvImagesDirPath/image_%04d.jpg"
    echo "ImageRecorder crashed Respawning after 5s.."
    sleep 5
done