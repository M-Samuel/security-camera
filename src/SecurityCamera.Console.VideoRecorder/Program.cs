using SecurityCamera.Application.Command.VideoRecorder;
using SecurityCamera.Console.VideoRecorder;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.VideoRecorderDomain;
using SecurityCamera.Infrastructure.AzureBlobStorage;

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
    hostApplicationBuilder.Services.AddSingleton<IVideoRecorderService, VideoRecorderService>();
}

static void RegisterApplication(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddSingleton<VideoRecorderCommand>();
}

static void RegisterInfrastructure(HostApplicationBuilder hostApplicationBuilder)
{
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
    if(string.IsNullOrWhiteSpace(configuration[nameof(EnvVars.AzureStorageConnectionString)]))
        throw new ArgumentNullException(nameof(EnvVars.AzureStorageConnectionString));
    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.RemoteStorageContainer)]))
        throw new ArgumentNullException(nameof(Args.RemoteStorageContainer));
    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.RemoteStorageVideoDirectory)]))
        throw new ArgumentNullException(nameof(Args.RemoteStorageVideoDirectory));
    if(string.IsNullOrWhiteSpace(configuration[nameof(Args.LocalVideoDirectory)]))
        throw new ArgumentNullException(nameof(Args.LocalVideoDirectory));
}