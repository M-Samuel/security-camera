using System.Drawing;
using System.Drawing.Drawing2D;
using ObjectDetector.YoloParser;
using ObjectDetector.DataStructures;
using ObjectDetector;
using Microsoft.ML;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var modelFilePath = "TinyYolo2_model.onnx";
string[] selectedLabels = new string[] { "person", "car", "dog" };

ConsumeCameraImages();
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

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

void DetectionAction(byte[] imageBytes, string imageName){
    // Initialize MLContext
    MLContext mlContext = new MLContext();
    string basePath = Path.GetTempPath();
    string imagesFolder = Path.Combine(basePath, Guid.NewGuid().ToString());
    Directory.CreateDirectory(imagesFolder);

    try
    {
        //string tempImageDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // Load Data
        IEnumerable<ImageNetData> imageArray = ImageNetData.ReadFromByteArray(imagesFolder, imageBytes, imageName);
        IDataView imageDataView = mlContext.Data.LoadFromEnumerable(imageArray);
    
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
        for (var i = 0; i < imageArray.Count(); i++)
        {
            string imageFileName = imageArray.ElementAt(i).Label;
            IList<YoloBoundingBox> detectedObjects = boundingBoxes.ElementAt(i);

            //DrawBoundingBox(imagesFolder, outputFolder, imageFileName, detectedObjects);

            LogDetectedObjects(imageFileName, detectedObjects);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
    finally
    {
        Directory.Delete(imagesFolder, true);
    }
}

async void ConsumeCameraImages()
{
    var factory = new ConnectionFactory { HostName = "localhost" };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    channel.QueueDeclare(queue: "CameraImages",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

    Console.WriteLine(" [*] Waiting for messages.");

    var consumer = new EventingBasicConsumer(channel);
    consumer.ConsumerCancelled += Consumer_ConsumerCancelled;
    consumer.Registered += Consumer_Registered;
    consumer.Received += (model, basicDeliverEventArgs) =>
    {
        string? cameraName = GetHeaderValue(basicDeliverEventArgs, "Camera");
        string? imageDateTime = GetHeaderValue(basicDeliverEventArgs, "CreatedDateTimeUTC");
        string? imageName = null;
        if(cameraName != null && imageDateTime != null){
            DateTime createdDateTimeUTC = DateTime.Parse(imageDateTime);
            imageName = $"{cameraName}_{createdDateTimeUTC.ToString("yyyyMMddHHmmss")}.png";
            Console.WriteLine($" [x] Received image from {cameraName} at {imageDateTime}.");
        }
    
        byte[] body = basicDeliverEventArgs.Body.ToArray();
        Console.WriteLine($" [x] Received {body.Length}");
        if(imageName != null)
            DetectionAction(body, imageName);
        else
            Console.WriteLine(" [x] No camera name or image date time found in the message header");
        Console.WriteLine(" [x] Done");

        // here channel could also be accessed as ((EventingBasicConsumer)sender).Model
        channel.BasicAck(deliveryTag: basicDeliverEventArgs.DeliveryTag, multiple: false);
    };
    channel.BasicConsume(queue: "CameraImages",
                        autoAck: false,
                        consumer: consumer);
    
    while(true){
        await Task.Delay(1000);
    }
}

void Consumer_Registered(object? sender, ConsumerEventArgs e)
{
}

void Consumer_ConsumerCancelled(object? sender, ConsumerEventArgs e)
{
    
}

static string? GetHeaderValue(BasicDeliverEventArgs basicDeliverEventArgs, string headerName){
    var headers = basicDeliverEventArgs.BasicProperties.Headers;
    if (headers != null && headers.ContainsKey(headerName))
    {
        var headerValue = Encoding.UTF8.GetString((byte[])headers[headerName]);
        return headerValue;
    }
    return null;
}

