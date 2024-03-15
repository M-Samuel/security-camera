using SecurityCamera.Application.Command.ImageRecorder;
using SecurityCamera.Console.ImageRecorder;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Infrastructure.AzureBlobStorage;
using SecurityCamera.Infrastructure.AzureServiceBus;
using SecurityCamera.Infrastructure.RabbitMq;

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
    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.CameraName)]))
        throw new ArgumentNullException(nameof(Args.CameraName));
    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.QueueName)]))
        throw new ArgumentNullException(nameof(Args.QueueName));
    // if(string.IsNullOrWhiteSpace(configuration[nameof(Args.RoutingKey)]))
    //     throw new ArgumentNullException(nameof(Args.RoutingKey));
    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.ImagesDirPath)]))
        throw new ArgumentNullException(nameof(Args.ImagesDirPath));
    // if(string.IsNullOrWhiteSpace(configuration[nameof(Args.RabbitMqHostName)]))
    //     throw new ArgumentNullException(nameof(Args.RabbitMqHostName));

    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.RemoteStorageContainer)]))
        throw new ArgumentNullException(nameof(Args.RemoteStorageContainer));
    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.RemoteStorageFileDirectory)]))
        throw new ArgumentNullException(nameof(Args.RemoteStorageFileDirectory));
}