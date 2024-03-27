#!/bin/bash

# record rtsp stream to image files each second 
/scripts/record.sh &
P1=$!

# push images to queue
/scripts/push.sh &
P2=$!

wait $P1 $P2