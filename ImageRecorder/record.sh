#!/bin/bash
while true
do

    image="/images/image_$(date -u +%Y%m%d%H%M%S).png"
    ffmpeg -re -rtsp_transport tcp -i $RTSP -frames:v 1 -vf "fps=$FPS" $image && echo "Image Generated to $image"
    sleep 1
done