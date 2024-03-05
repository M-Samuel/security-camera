#!/bin/bash

#Resize Images
/scripts/imageResize.sh &
P1=$!

# record rtsp stream to image files each second 
/scripts/record.sh &
P2=$!

# push images to rabbitmq
/scripts/push.sh &
P3=$!

wait $P1 $P2 $P3