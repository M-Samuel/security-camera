#!/bin/bash

original_dir="/images/original"
resized_dir="/images/resized"

mkdir $original_dir
mkdir $resized_dir

while true
do

    for file in "$original_dir"/*.png; do
        if [[ -f "$file" ]]; then
            resized_file="$resized_dir/$(basename "$file" .png).jpg"
            convert "$file" -resize 1024x768 "$resized_file"
            rm $file
            echo "File $file has been resized and saved as $resized_file"
        fi
        sleep 1;
    done

sleep 5
done
