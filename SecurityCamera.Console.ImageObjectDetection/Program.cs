using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SecurityCamera.Application.Command.ObjectDetection;
using SecurityCamera.Console.ImageObjectDetection;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain;
using SecurityCamera.Domain.ObjectDetectionDomain.Repository;
using SecurityCamera.Infrastructure.AzureBlobStorage;
using SecurityCamera.Infrastructure.AzureServiceBus;
using SecurityCamera.Infrastructure.Database.Contexts;
using SecurityCamera.Infrastructure.Database.Repositories;
using SecurityCamera.Infrastructure.RabbitMq;
using SecurityCamera.SharedKernel;

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
    hostApplicationBuilder.Services.AddSingleton<IObjectDetectionService, ObjectDetectionService>();
}

static void RegisterApplication(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddScoped<ObjectDetectionCommand>();
}

static void RegisterInfrastructure(HostApplicationBuilder hostApplicationBuilder)
{
    hostApplicationBuilder.Services.AddSingleton<IQueuePublisherService<DetectionMessage>, AzureServiceBusService>();
    hostApplicationBuilder.Services.AddSingleton<IQueueConsumerService<ImageRecorderOnImagePushMessage>, AzureServiceBusService>();
    hostApplicationBuilder.Services.AddSingleton<IRemoteStorageService, AzureBlobStorageService>();
    

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
    hostApplicationBuilder.Services.AddSingleton<IServiceProvider>(sp => sp);
}