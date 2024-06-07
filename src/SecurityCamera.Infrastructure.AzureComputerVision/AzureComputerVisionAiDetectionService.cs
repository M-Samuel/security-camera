using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;

namespace SecurityCamera.Infrastructure.AzureComputerVision;

public class AzureComputerVisionAiDetectionService : IAiDetectionService
{
    private readonly ImageAnalysisClient _client;
    private readonly ILogger<AzureComputerVisionAiDetectionService> _logger;

    public AzureComputerVisionAiDetectionService(IConfiguration configuration, ILogger<AzureComputerVisionAiDetectionService> logger)
    {
        _logger = logger;
        string endpoint = configuration[nameof(EnvVars.AzureComputerVisionEndpoint)] ?? string.Empty;
        string key = configuration[nameof(EnvVars.AzureComputerVisionKey)] ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(nameof(EnvVars.AzureComputerVisionEndpoint)))
            throw new ArgumentNullException(nameof(EnvVars.AzureComputerVisionEndpoint));
        if (string.IsNullOrWhiteSpace(nameof(EnvVars.AzureComputerVisionKey)))
            throw new ArgumentNullException(nameof(EnvVars.AzureComputerVisionKey));
        
        _client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
    }
    public async Task<DetectionEvent[]> AnalyseImage(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken)
    {
        ImageAnalysisResult result = await _client.AnalyzeAsync(
            BinaryData.FromBytes(imageRecordedEvent.ImageBytes),
            VisualFeatures.Objects |
            VisualFeatures.People,
            cancellationToken: cancellationToken);

        List<DetectionEvent> detectionEvents = new List<DetectionEvent>();
        
        _logger.LogInformation($"Image analysis results:");
        _logger.LogInformation($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        _logger.LogInformation($" Objects:");
        foreach (DetectedObject detectedObject in result.Objects.Values)
        {
            if(detectedObject.Tags.First().Name != "car")
                continue;
            
            _logger.LogInformation($"   Object: '{detectedObject.Tags.First().Name}', Bounding box {detectedObject.BoundingBox}");
            detectionEvents.Add(
                new DetectionEvent( 
                    DateTime.UtcNow, 
                    imageRecordedEvent.CameraName,
                    imageRecordedEvent.ImageBytes,
                    imageRecordedEvent.ImageName,
                    imageRecordedEvent.ImageCreatedDateTime,
                    detectedObject.Tags.First().Name,
                    detectedObject.Tags.First().Confidence,
                    detectedObject.BoundingBox.X,
                    detectedObject.BoundingBox.Y,
                    detectedObject.BoundingBox.Width,
                    detectedObject.BoundingBox.Height
                )
            );
        }

        _logger.LogInformation($" People:");
        foreach (DetectedPerson person in result.People.Values)
        {
            _logger.LogInformation($"   Person: Bounding box {person.BoundingBox}, Confidence: {person.Confidence:F4}");
            
            detectionEvents.Add(
                new DetectionEvent( 
                    DateTime.UtcNow, 
                    imageRecordedEvent.CameraName,
                    imageRecordedEvent.ImageBytes,
                    imageRecordedEvent.ImageName,
                    imageRecordedEvent.ImageCreatedDateTime,
                    "person",
                    person.Confidence,
                    person.BoundingBox.X,
                    person.BoundingBox.Y,
                    person.BoundingBox.Width,
                    person.BoundingBox.Height
                )
            );
        }

        return detectionEvents.ToArray();
    }
}