#!/bin/bash
while true
do
ffmpeg -re -rtsp_transport tcp -i $RTSP  -c:v copy -c:a aac -reset_timestamps 1 -f segment -segment_time $SEG_TIME -strftime 1 -bufsize 20M $RECORDING_DIRECTORY/stream_piece_%s.mp4
echo "ffmpeg crashed Respawning after 5s.."
sleep 5
done
