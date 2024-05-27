# Copyright 2021 The TensorFlow Authors. All Rights Reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
"""Main script to run the object detection routine."""
import argparse
import sys
import time
import json

import cv2
from tflite_support.task import core
from tflite_support.task import processor
from tflite_support.task import vision


def run(model: str, num_threads: int, enable_edgetpu: bool, image_path:str) -> None:
  """Continuously run inference on images acquired from the camera.

  Args:
    model: Name of the TFLite object detection model.
    num_threads: The number of CPU threads to run the model.
    enable_edgetpu: True/False whether the model is a EdgeTPU model.
  """

  # Initialize the object detection model
  base_options = core.BaseOptions(
      file_name=model, use_coral=enable_edgetpu, num_threads=num_threads)
  detection_options = processor.DetectionOptions(
      max_results=10, score_threshold=0.3)
  options = vision.ObjectDetectorOptions(
      base_options=base_options, detection_options=detection_options)
  detector = vision.ObjectDetector.create_from_options(options)
  
  image = cv2.imread(image_path)
  
  # Convert the image from BGR to RGB as required by the TFLite model.
  rgb_image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

  # Create a TensorImage object from the RGB image.
  input_tensor = vision.TensorImage.create_from_array(rgb_image)

  # Run object detection estimation using the model.
  detection_result = detector.detect(input_tensor)

  detections = []
  for detection in detection_result.detections:
    box = detection.bounding_box
    for category in detection.categories:
      d = {}
      d['category_name'] = category.category_name
      d['score'] = category.score
      d['origin_x'] = box.origin_x
      d['origin_y'] = box.origin_y
      d['width'] = box.width
      d['height'] = box.height
      detections.append(d)
  
  print(json.dumps(detections, indent=4))



def main():
  parser = argparse.ArgumentParser(
      formatter_class=argparse.ArgumentDefaultsHelpFormatter)
  parser.add_argument(
      '--model',
      help='Path of the object detection model.',
      required=False,
      default='efficientdet_lite0.tflite')
  parser.add_argument(
      '--imagePath',
      help='Path of the image',
      required=True)
  parser.add_argument(
      '--numThreads',
      help='Number of CPU threads to run the model.',
      required=False,
      type=int,
      default=4)
  parser.add_argument(
      '--enableEdgeTPU',
      help='Whether to run the model on EdgeTPU.',
      action='store_true',
      required=False,
      default=False)
  args = parser.parse_args()

  run(args.model, int(args.numThreads), bool(args.enableEdgeTPU), args.imagePath)


if __name__ == '__main__':
  main()