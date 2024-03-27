#!/bin/bash
while true
do
    retries=0
    while true
    do
        if ! pgrep -x "ffmpeg" > /dev/null; then
            echo "ffmpeg is not running, waiting for 10s.."
            sleep 10
            retries=$((retries+1))
            if [ $retries -eq 3 ]; then
                echo "Reached maximum retries, exiting.."
                echo "ffmpeg is not running, exiting.."
                break 2
            fi
        else
            break
        fi
    done

    /scripts/rename.sh
    aws s3 sync --storage-class $STORAGE_CLASS $UPLOAD_DIRECTORY s3://$S3_BUCKET/$S3_DIRECTORY && rm -f $UPLOAD_DIRECTORY/*
    sleep $SYNC_S3_AFTER_SECONDS
    echo "S3 Bucket Upload Respawning.."
done
