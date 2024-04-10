using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;

namespace SecurityCamera.Infrastructure.UltralyticsAi;

public class UltralyticsAiService : IAiDetectionService
{
    private readonly string? _modelName;

    public UltralyticsAiService(IConfiguration configuration)
    {
        _modelName = "yolov5nu.pt";
        
        if(!string.IsNullOrWhiteSpace(configuration[nameof(Args.ModelName)]))
            _modelName = configuration[nameof(Args.ModelName)];
    }
    
    public async Task<DetectionEvent[]> AnalyseImage(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken)
    {
        string tempImagePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()+Path.GetExtension(imageRecordedEvent.ImageName));
        try{
            await WriteImageToDisk(imageRecordedEvent.ImageBytes, tempImagePath, cancellationToken);
            string result = await RunUltralyticsAi(tempImagePath, cancellationToken);
            if(string.IsNullOrWhiteSpace(result))
                return Array.Empty<DetectionEvent>();
            
            string[]? detections = JsonSerializer.Deserialize<string[]>(result);
            if(detections == null)
                return Array.Empty<DetectionEvent>();
            
            return detections.Select(detection => {
                return new DetectionEvent(
                    CameraName: imageRecordedEvent.CameraName,
                    OccurrenceDateTime: DateTime.UtcNow, 
                    ImageBytes: imageRecordedEvent.ImageBytes,
                    ImageName: imageRecordedEvent.ImageName,
                    ImageCreatedDateTime: imageRecordedEvent.ImageCreatedDateTime,
                    DetectionData: detection
                );
            }).ToArray();
        }
        finally
        {
            File.Delete(tempImagePath);
        }

    }

    private async Task WriteImageToDisk(byte[] imageBytes, string imagePath, CancellationToken cancellationToken)
    {
        await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken);
    }

    private async Task<string> RunUltralyticsAi(string imagePath, CancellationToken cancellationToken)
    {
        string parserPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "Output_parser.py");
        string command = $"yolo predict model={_modelName} source='{imagePath}' | python {parserPath}";
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        string result = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return result;
    }
}
