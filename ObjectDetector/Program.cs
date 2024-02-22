using System.Drawing;
using System.Drawing.Drawing2D;
using ObjectDetector.YoloParser;
using ObjectDetector.DataStructures;
using ObjectDetector;
using Microsoft.ML;

var modelFilePath = "TinyYolo2_model.onnx";

// Initialize MLContext
MLContext mlContext = new MLContext();
string[] selectedLabels = new string[] { "person", "car", "dog" };

try
{
    //string tempImageDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    string imagesFolder = args[0];
    // Load Data
    IEnumerable<ImageNetData> images = ImageNetData.ReadFromFile(imagesFolder);
    IDataView imageDataView = mlContext.Data.LoadFromEnumerable(images);

    // Create instance of model scorer
    var modelScorer = new OnnxModelScorer(imagesFolder, modelFilePath, mlContext);

    // Use model to score data
    IEnumerable<float[]> probabilities = modelScorer.Score(imageDataView);

    // Post-process model output
    YoloOutputParser parser = new YoloOutputParser();

    var boundingBoxes =
        probabilities
        .Select(probability => parser.ParseOutputs(probability, 0.2F))
        .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F));

    // Draw bounding boxes for detected objects in each of the images
    for (var i = 0; i < images.Count(); i++)
    {
        string imageFileName = images.ElementAt(i).Label;
        IList<YoloBoundingBox> detectedObjects = boundingBoxes.ElementAt(i);

        //DrawBoundingBox(imagesFolder, outputFolder, imageFileName, detectedObjects);

        LogDetectedObjects(imageFileName, detectedObjects);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}

Console.WriteLine("========= End of Process..Hit any Key ========");


void LogDetectedObjects(string imageName, IList<YoloBoundingBox> boundingBoxes)
{
    Console.WriteLine($".....The objects in the image {imageName} are detected as below....");

    foreach (var box in boundingBoxes)
    {
        if(selectedLabels.Contains(box.Label))
            Console.WriteLine($"{box.Label} and its Confidence score: {box.Confidence}");
    }

    Console.WriteLine("");
}
