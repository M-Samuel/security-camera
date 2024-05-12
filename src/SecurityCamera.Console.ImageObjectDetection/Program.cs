using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SecurityCamera.Application.Command.ObjectDetection;
using SecurityCamera.Console.ImageObjectDetection;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;
using SecurityCamera.Infrastructure.AzureBlobStorage;
using SecurityCamera.Infrastructure.AzureComputerVision;
using SecurityCamera.Infrastructure.AzureServiceBus;
using SecurityCamera.Infrastructure.Database.Contexts;
using SecurityCamera.Infrastructure.Database.Repositories;
using SecurityCamera.SharedKernel;

using BlobEnvVars = SecurityCamera.Infrastructure.AzureBlobStorage.EnvVars;
using BusEnvVars = SecurityCamera.Infrastructure.AzureServiceBus.EnvVars;
using VisionEnvVars = SecurityCamera.Infrastructure.AzureComputerVision.EnvVars;
using DomainArgs = SecurityCamera.Domain.ObjectDetectionDomain.Args;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

RegisterDomainServices(builder);
RegisterApplication(builder);
RegisterInfrastructure(builder);
RegisterCrossCuttingConcerns(builder);

ValidateArgs(builder.Configuration);
var host = builder.Build();
host.Run();


static void RegisterDomainServices(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddScoped<IObjectDetectionService, ObjectDetectionService>();
}

static void RegisterApplication(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddSingleton<ObjectDetectionCommand>();
}

static void RegisterInfrastructure(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddSingleton<IQueuePublisherService<DetectionMessage>, AzureServiceBusService>();
    hostApplicationBuilder.Services.AddSingleton<IQueueConsumerService<ImageRecorderOnImagePushMessage>, AzureServiceBusService>();
    hostApplicationBuilder.Services.AddSingleton<IRemoteStorageService, AzureBlobStorageService>();

    hostApplicationBuilder.Services.AddSingleton<IAiDetectionService, AzureComputerVisionAiDetectionService>();
    
    hostApplicationBuilder.Services.AddDbContext<DatabaseContext>(
    options => options
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole().AddDebug()))
            // .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Test")
            .UseInMemoryDatabase("SecurityCameraDb")
            .EnableDetailedErrors()
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

    #pragma warning disable CS8603 // Possible null reference return.
    hostApplicationBuilder.Services.AddScoped<IUnitOfWork>(s => s.GetService<DatabaseContext>());
    #pragma warning restore CS8603 // Possible null reference return.

    hostApplicationBuilder.Services.AddScoped<IObjectDetectionReadRepository, ObjectDetectionRepository>();
    hostApplicationBuilder.Services.AddScoped<IObjectDetectionWriteRepository, ObjectDetectionRepository>();
}

static void RegisterCrossCuttingConcerns(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Logging.ClearProviders();
    hostApplicationBuilder.Logging.AddConsole();
    hostApplicationBuilder.Logging.AddDebug();
}

static void ValidateArgs(IConfiguration configuration)
{
    if(string.IsNullOrWhiteSpace(configuration[nameof(BusEnvVars.AzureServiceBusConnectionString)]))
        throw new ArgumentNullException(nameof(BusEnvVars.AzureServiceBusConnectionString));
    if(string.IsNullOrWhiteSpace(configuration[nameof(BlobEnvVars.AzureStorageConnectionString)]))
        throw new ArgumentNullException(nameof(BlobEnvVars.AzureStorageConnectionString));
    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.RemoteStorageFileDirectory)]))
        throw new ArgumentNullException(nameof(DomainArgs.RemoteStorageFileDirectory));

    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.ServiceBusQueueImageRecords)]))
        throw new ArgumentNullException(nameof(DomainArgs.ServiceBusQueueImageRecords));
    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.ServiceBusQueueDetections)]))
        throw new ArgumentNullException(nameof(DomainArgs.ServiceBusQueueDetections));
    if(string.IsNullOrWhiteSpace(configuration[nameof(VisionEnvVars.AzureComputerVisionEndpoint)]))
        throw new ArgumentNullException(nameof(VisionEnvVars.AzureComputerVisionEndpoint));
    if(string.IsNullOrWhiteSpace(configuration[nameof(VisionEnvVars.AzureComputerVisionKey)]))
        throw new ArgumentNullException(nameof(VisionEnvVars.AzureComputerVisionKey));
}