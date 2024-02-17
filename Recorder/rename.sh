#!/bin/bash

# Define source and target directories
SOURCE_DIR=$RECORDING_DIRECTORY
TARGET_DIR=$UPLOAD_DIRECTORY

# Check if target directory exists, if not, create it
if [ ! -d "$TARGET_DIR" ]; then
    mkdir -p "$TARGET_DIR"
    echo "Created target directory: $TARGET_DIR"
fi

echo "Finding files in the source directory that have not been modified in the last minute..."
# Find files in the source directory that have not been modified in the last minute
# and move them to the target directory
find "$SOURCE_DIR" -type f -mmin $UPLOAD_FILE_OLDER_MIN -exec mv {} "$TARGET_DIR" \;



# Check all files in target directory
for file in "$TARGET_DIR"/*; do
    if [[ $file =~ stream_piece_([0-9]{10})\.mp4 ]]; then
        timestamp=${BASH_REMATCH[1]}
        # Convert the unix timestamp to yyyyMMddHHmmss using the Mauritian time zone
        formatted_timestamp=$(TZ=Indian/Mauritius date -d @$timestamp +'%Y%m%d%H%M%S')
        # Rename the file
        echo "Renaming file: $file to stream_piece_$formatted_timestamp.mp4"
        mv "$file" "$TARGET_DIR/stream_piece_$formatted_timestamp.mp4"
    fi
done
