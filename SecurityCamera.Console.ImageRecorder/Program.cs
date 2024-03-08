using SecurityCamera.Application.Command.ImageRecorder;
using SecurityCamera.Console.ImageRecorder;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.ImageRecorderDomain.Repository;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Infrastructure.Database;
using SecurityCamera.Infrastructure.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
RegisterDomainServices(builder);
RegisterApplication(builder);
RegisterInfrastructure(builder);
RegisterCrossCuttingConcerns(builder);

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
    hostApplicationBuilder.Services.AddSingleton<IQueuePublisherService, RabbitMqService>();
    hostApplicationBuilder.Services.AddSingleton<IImageRecorderWriteRepository, FakeRepository>();
}

static void RegisterCrossCuttingConcerns(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Logging.ClearProviders();
    hostApplicationBuilder.Logging.AddConsole();
    hostApplicationBuilder.Logging.AddDebug();
}
