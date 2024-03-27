#!/bin/bash

while true
do
    # ffmpeg -re -stream_loop -1 -i video.mp4 -f rtsp rtsp://localhost:8554/stream
    ffmpeg -re -stream_loop -1 -i video.mp4 -f rtsp $RTSP_URL
    sleep 5
done