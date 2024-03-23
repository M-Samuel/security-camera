using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;
using SecurityCamera.Infrastructure.OnnxAi.DataStructures;
using SecurityCamera.Infrastructure.OnnxAi.YoloParser;

namespace SecurityCamera.Infrastructure.OnnxAi;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class OnnxAiService : IAiDetectionService
{
    public OnnxAiService(ILogger<OnnxAiService> logger)
    {
        _logger = logger;
        _selectedLabels = ConvertEnumToStringArray<DetectionType>().ToArray();
    }

    private readonly string _modelFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "TinyYolo2_model.onnx") ;
    private readonly string[] _selectedLabels;
    private readonly ILogger<OnnxAiService> _logger;

    public IEnumerable<DetectionEvent> AnalyseImage(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken)
    {
        IList<YoloBoundingBox>[] detections = DetectionAction(imageRecordedEvent.ImageBytes, imageRecordedEvent.ImageName);
        if (detections.Length == 0)
            yield break;

        foreach (var image in detections)
        {
            foreach (var detectionBox in image)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                string? detectionType = _selectedLabels.FirstOrDefault(dt =>
                    dt.ToLowerInvariant() == detectionBox.Label.ToLowerInvariant());
                
                _logger.LogInformation($".....The objects in the image {imageRecordedEvent.ImageName} are detected as below....");
                
                if(detectionType == null)
                    continue;
                
                _logger.LogInformation($"{detectionBox.Label} and its Confidence score: {detectionBox.Confidence}");
                
                DetectionEvent detectionEvent = new(
                    CameraName: imageRecordedEvent.CameraName,
                    OccurrenceDateTime: DateTime.UtcNow, 
                    ImageBytes: imageRecordedEvent.ImageBytes,
                    ImageName: imageRecordedEvent.ImageName,
                    ImageCreatedDateTime: imageRecordedEvent.ImageCreatedDateTime,
                    DetectionType: ConvertStringToEnum<DetectionType>(detectionType));
                
                yield return detectionEvent;
            }
        }
    }
    

    void DrawBoundingBox(string inputImageLocation, string outputImageLocation, string imageName,
        IList<YoloBoundingBox> filteredBoundingBoxes)
    {
        Image image = Image.FromFile(Path.Combine(inputImageLocation, imageName));

        var originalImageHeight = image.Height;
        var originalImageWidth = image.Width;

        foreach (var box in filteredBoundingBoxes)
        {
            // Get Bounding Box Dimensions
            var x = (uint)Math.Max(box.Dimensions.X, 0);
            var y = (uint)Math.Max(box.Dimensions.Y, 0);
            var width = (uint)Math.Min(originalImageWidth - x, box.Dimensions.Width);
            var height = (uint)Math.Min(originalImageHeight - y, box.Dimensions.Height);

            // Resize To Image
            x = (uint)originalImageWidth * x / OnnxModelScorer.ImageNetSettings.imageWidth;
            y = (uint)originalImageHeight * y / OnnxModelScorer.ImageNetSettings.imageHeight;
            width = (uint)originalImageWidth * width / OnnxModelScorer.ImageNetSettings.imageWidth;
            height = (uint)originalImageHeight * height / OnnxModelScorer.ImageNetSettings.imageHeight;

            // Bounding Box Text
            string text = $"{box.Label} ({(box.Confidence * 100):0}%)";

            using Graphics thumbnailGraphic = Graphics.FromImage(image);
            thumbnailGraphic.CompositingQuality = CompositingQuality.HighQuality;
            thumbnailGraphic.SmoothingMode = SmoothingMode.HighQuality;
            thumbnailGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Define Text Options
            Font drawFont = new Font("Arial", 12, FontStyle.Bold);
            SizeF size = thumbnailGraphic.MeasureString(text, drawFont);
            SolidBrush fontBrush = new SolidBrush(Color.Black);
            Point atPoint = new Point((int)x, (int)y - (int)size.Height - 1);

            // Define BoundingBox options
            Pen pen = new Pen(box.BoxColor, 3.2f);
            SolidBrush colorBrush = new SolidBrush(box.BoxColor);

            // Draw text on image 
            thumbnailGraphic.FillRectangle(colorBrush, (int)x, (int)(y - size.Height - 1), (int)size.Width,
                (int)size.Height);
            thumbnailGraphic.DrawString(text, drawFont, fontBrush, atPoint);

            // Draw bounding box on image
            thumbnailGraphic.DrawRectangle(pen, x, y, width, height);
        }

        if (!Directory.Exists(outputImageLocation))
            Directory.CreateDirectory(outputImageLocation);

        image.Save(Path.Combine(outputImageLocation, imageName));
    }

    private IList<YoloBoundingBox>[] DetectionAction(byte[] imageBytes, string imageName)
    {
        // Initialize MLContext
        MLContext mlContext = new MLContext();
        string basePath = Path.GetTempPath();
        string imagesFolder = Path.Combine(basePath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(imagesFolder);

        try
        {
            // Load Data
            IEnumerable<ImageNetData> imageArray =
                ImageNetData.ReadFromByteArray(imagesFolder, imageBytes, imageName).ToArray();
            IDataView imageDataView = mlContext.Data.LoadFromEnumerable(imageArray);

            // Create instance of model scorer
            var modelScorer = new OnnxModelScorer(imagesFolder, _modelFilePath, mlContext);

            // Use model to score data
            IEnumerable<float[]> probabilities = modelScorer.Score(imageDataView);

            // Post-process model output
            YoloOutputParser parser = new YoloOutputParser();

            IList<YoloBoundingBox>[] boundingBoxes =
                probabilities
                    .Select(probability => parser.ParseOutputs(probability, 0.2F))
                    .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F))
                    .ToArray();

            // Draw bounding boxes for detected objects in each of the images
            // for (var i = 0; i < imageArray.Count(); i++)
            // {
            //     string imageFileName = imageArray.ElementAt(i).Label;
            //     IList<YoloBoundingBox> detectedObjects = boundingBoxes.ElementAt(i);
            //
            //     DrawBoundingBox(imagesFolder, "bin/Debug/net8.0/output", imageFileName, detectedObjects);
            // }

            return boundingBoxes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detection failed");
            throw;
        }
        finally
        {
            //Directory.Delete(imagesFolder, true);
        }
    }
    
    private static string[] ConvertEnumToStringArray<T>() where T : Enum
    {
        return Enum.GetNames(typeof(T));
    }
    
    private static T ConvertStringToEnum<T>(string value) where T : Enum
    {
        return (T) Enum.Parse(typeof(T), value, true); // The 'true' parameter makes the parsing case-insensitive
    }
}