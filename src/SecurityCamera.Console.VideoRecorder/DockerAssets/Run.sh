#!/bin/bash

# record rtsp stream to file 
/scripts/record.sh &
P1=$!

# upload file to remote storage and delete old files
/scripts/push.sh &
P2=$!

wait $P1 $P2