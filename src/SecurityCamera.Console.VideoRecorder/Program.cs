using SecurityCamera.Console.VideoRecorder;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Infrastructure.AzureBlobStorage;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
RegisterInfrastructure(builder);

ValidateArgs(builder.Configuration);
var host = builder.Build();
host.Run();


static void RegisterInfrastructure(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddSingleton<IRemoteStorageService, AzureBlobStorageService>();
}


static void ValidateArgs(IConfiguration configuration)
{
    if(string.IsNullOrWhiteSpace(configuration[nameof(EnvVars.AzureStorageConnectionString)]))
        throw new ArgumentNullException(nameof(EnvVars.AzureStorageConnectionString));
}