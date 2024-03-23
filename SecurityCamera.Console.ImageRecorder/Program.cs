using SecurityCamera.Application.Command.ImageRecorder;
using SecurityCamera.Console.ImageRecorder;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Infrastructure.AzureBlobStorage;
using SecurityCamera.Infrastructure.AzureServiceBus;
using RabbitMqArgs = SecurityCamera.Infrastructure.RabbitMq.Args;
using BlobEnvVars = SecurityCamera.Infrastructure.AzureBlobStorage.EnvVars;
using BusEnvVars = SecurityCamera.Infrastructure.AzureServiceBus.EnvVars;
using DomainArgs = SecurityCamera.Domain.ImageRecorderDomain.Args;

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
    hostApplicationBuilder.Services.AddSingleton<IImageRecorderService, ImageRecorderService>();
}

static void RegisterApplication(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddSingleton<ImageRecorderCommand>();
}

static void RegisterInfrastructure(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddSingleton<IQueuePublisherService<ImageRecorderOnImagePushMessage>, AzureServiceBusService>();
    hostApplicationBuilder.Services.AddSingleton<IRemoteStorageService, AzureBlobStorageService>();
}

static void RegisterCrossCuttingConcerns(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Logging.ClearProviders();
    hostApplicationBuilder.Logging.AddConsole();
    hostApplicationBuilder.Logging.AddDebug();
}



static void ValidateArgs(IConfiguration configuration)
{
    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.CameraName)]))
        throw new ArgumentNullException(nameof(DomainArgs.CameraName));
    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.ServiceBusQueueImageRecords)]))
        throw new ArgumentNullException(nameof(DomainArgs.ServiceBusQueueImageRecords));
    // if(string.IsNullOrWhiteSpace(configuration[nameof(RabbitMqArgs.RoutingKey)]))
    //     throw new ArgumentNullException(nameof(RabbitMqArgs.RoutingKey));
    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.ImagesDirPath)]))
        throw new ArgumentNullException(nameof(DomainArgs.ImagesDirPath));
    // if(string.IsNullOrWhiteSpace(configuration[nameof(RabbitMqArgs.RabbitMqHostName)]))
    //     throw new ArgumentNullException(nameof(RabbitMqArgs.RabbitMqHostName));

    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.RemoteStorageContainer)]))
        throw new ArgumentNullException(nameof(DomainArgs.RemoteStorageContainer));
    if(string.IsNullOrWhiteSpace(configuration[nameof(DomainArgs.RemoteStorageFileDirectory)]))
        throw new ArgumentNullException(nameof(DomainArgs.RemoteStorageFileDirectory));
    if(string.IsNullOrWhiteSpace(configuration[nameof(BlobEnvVars.AzureStorageConnectionString)]))
        throw new ArgumentNullException(nameof(BlobEnvVars.AzureStorageConnectionString));
    if(string.IsNullOrWhiteSpace(configuration[nameof(BusEnvVars.AzureServiceBusConnectionString)]))
        throw new ArgumentNullException(nameof(BusEnvVars.AzureServiceBusConnectionString));
}