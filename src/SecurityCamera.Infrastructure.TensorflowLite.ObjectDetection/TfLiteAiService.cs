﻿using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain.Events;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain.Events;

namespace SecurityCamera.Infrastructure.TensorflowLite.ObjectDetection;

public class TfLiteAiService : IAiDetectionService
{
    private readonly string _scriptPath;
    private readonly ILogger<TfLiteAiService> _logger;
    private readonly string [] _allowedCategories = { "person", "car" };
    
    public TfLiteAiService(IConfiguration configuration, ILogger<TfLiteAiService> logger)
    {
        _scriptPath = configuration[nameof(EnvVars.TensorflowLiteDetectScriptPath)] ?? "";
        _logger = logger;
    }

    //{"RemoteStorageContainer":"image-upload-dev","RemoteStorageFilePath":"FrontDoor/bus.png","CameraName":"Test","ImageName":"bus.png","ImageCreatedDateTime":"2024-05-12T09:35:48.806719Z","QueueName":"dev-image-recordings"}
    public async Task<DetectionEvent[]> AnalyseImage(ImageRecordedEvent imageRecordedEvent, CancellationToken cancellationToken)
    {
        string imagePath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(imagePath, imageRecordedEvent.ImageBytes);
        TfDetection[]? detections = await LaunchTfLiteScript(imagePath, cancellationToken);
        File.Delete(imagePath);
        if(detections == null)
            return Array.Empty<DetectionEvent>();
        

        _logger.LogInformation($"Detection events raw: {JsonSerializer.Serialize(detections)}");

        TfDetection[] filteredDetections = detections
        .Where(tfd => _allowedCategories.Contains(tfd.CategoryName.ToLowerInvariant()) ).ToArray();

        _logger.LogInformation($"Detection events after filtering: {JsonSerializer.Serialize(filteredDetections)}");

        DetectionEvent[] detectionEvents = filteredDetections
        .Select(tfd => new DetectionEvent(
            DateTime.UtcNow,
            CameraName: imageRecordedEvent.CameraName,
            ImageBytes: imageRecordedEvent.ImageBytes,
            ImageCreatedDateTime: imageRecordedEvent.ImageCreatedDateTime,
            ImageName: imageRecordedEvent.ImageName,
            DetectionData: tfd.CategoryName
        )).ToArray();


        return detectionEvents;
    }


    private async Task<TfDetection[]?> LaunchTfLiteScript(string imagePath, CancellationToken cancellationToken)
    {
        string resultJsonPath = Path.GetTempFileName();

        ProcessStartInfo start = new ProcessStartInfo
        {
            WorkingDirectory = Path.GetDirectoryName(_scriptPath),
            FileName = "python", // Specify the path to the Python interpreter
            Arguments =  $"{_scriptPath} --imagePath {imagePath} --resultJsonPath {resultJsonPath}", // Specify the path to the Python script and any arguments
            UseShellExecute = false, // Do not use OS shell
            RedirectStandardOutput = true, // Any output, generated by application will be redirected back
            RedirectStandardError = true, // Any error, generated by application will be redirected back
            CreateNoWindow = true // Don't create new window
        };
        using (Process? process = Process.Start(start))
        {
            if(process == null)
                throw new ArgumentNullException(nameof(process));
            
            await process.WaitForExitAsync(cancellationToken);

            if(process.ExitCode != 0)
                throw new InvalidOperationException($"Tf Failed: {process.StandardOutput.ReadToEnd()}, {process.StandardError.ReadToEnd()}");

        }

        string jsonText = await File.ReadAllTextAsync(resultJsonPath, cancellationToken);
        TfDetection[]? result = JsonSerializer.Deserialize<TfDetection[]>(jsonText);
        File.Delete(resultJsonPath);

        return result;
    }
}

internal class TfDetection
{
    public required string CategoryName { get; set; }
    public required double Score { get; set; }
    public required int OriginX { get; set; }
    public required int OriginY { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
}