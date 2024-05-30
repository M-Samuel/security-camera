
# Security Camera Project

Disclosure: This is a personal project and should not be used for commercial use.

### Motivation
While recording security camera footage, the application should be able to storage the videos on a cloud provider and should be able to analyse in near real time images to detect object like car and/or person.


### Infrastructure
* RTSP capable camera
* Raspberry 4b for recording and uploading images/videos
* Azure Container Apps to analyse footage
* Azure Service Bus to integrate uploading and detection events
* Azure Storage Account to store video and images
* Azure Computer Vision (optional) to analyse image via Object detection API

### Techstack
* DOTNET 8
* FFMPEG for image/video recording 
* Docker
* Tensorflow Lite with efficientdet_lite model (Python 3.9)
* Linux Bash scripts

### Resources for AI Object detection
* [Detect objects using ONNX in ML.NET (Tried but not currently used)](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx)
* [Ultralytics (Tried but not currently used)](https://docs.ultralytics.com/quickstart/#__tabbed_2_3)
* [Azure Computer vision sample (Implemented not currently used)](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/vision/Azure.AI.Vision.ImageAnalysis/samples/Sample04_Objects.md)
* [Tensorflow Lite (Implemented, code adapted for current use case)](https://github.com/tensorflow/examples/tree/master/lite/examples/object_detection/raspberry_pi)


### Application Images 
1. VideoRecorder - Records videos and uploads them to Azure storage
1. ImageRecorder - Records images and uploads them to Azure storage, push image recorded events to Azure service Bus
1. ImageObjectDetection - Consume messages from image queue and run AI detection algorithm
