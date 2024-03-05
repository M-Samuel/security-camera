#!/bin/bash

original_dir="/images/original"
resized_dir="/images/resized"

while true
do
    for file in "$original_dir"/*.png; do
        if [[ -f "$file" && $(find "$file" -not -newermt '5 seconds ago' 2>/dev/null) ]]; then
            resized_file="$resized_dir/$(basename "$file")"
            convert "$file" -resize 1024x768 "$resized_file"
            rm $file
            echo "File $file has been resized and saved as $resized_file"
        fi
    done
sleep 5
done
