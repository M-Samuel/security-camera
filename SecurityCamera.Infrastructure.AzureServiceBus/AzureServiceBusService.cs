using SecurityCamera.Domain.InfrastructureServices;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.ObjectDetectionDomain;

namespace SecurityCamera.Infrastructure.AzureServiceBus;

public class AzureServiceBusService : 
IQueueConsumerService<ImageRecorderOnImagePushMessage>, 
IQueuePublisherService<ImageRecorderOnImagePushMessage>,
IQueueConsumerService<DetectionMessage>, 
IQueuePublisherService<DetectionMessage>
{
    private readonly ILogger<AzureServiceBusService> _logger;
    private readonly ServiceBusClient _client;

    private readonly object _objectLock;

    private Task? _imageRecorderConsumerTask;
    private Task? _detectionConsumerTask;

    public AzureServiceBusService(IConfiguration configuration, ILogger<AzureServiceBusService> logger)
    {
        ServiceBusClientOptions options = new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };
        _objectLock = new();
        _logger = logger;
        _client = new ServiceBusClient(configuration[nameof(EnvVars.AzureServiceBusConnectionString)], options);
    }
    public void Dispose()
    {
        Task.Factory.StartNew(async () => await _client.DisposeAsync());
    }

    private event EventHandler<ImageRecorderOnImagePushMessage>? ImageRecorderMessageReceived;
    event EventHandler<ImageRecorderOnImagePushMessage>? IQueueConsumerService<ImageRecorderOnImagePushMessage>.MessageReceived
    {
        add {
            lock (_objectLock)
            {
                ImageRecorderMessageReceived -= value;
                ImageRecorderMessageReceived += value;
            }
                
        }
        remove
        {
            lock (_objectLock)
                ImageRecorderMessageReceived -= value;
        }
    }

    public async Task GetMessageFromQueue(string queueName, EventHandler<ImageRecorderOnImagePushMessage> subscriber, CancellationToken cancellationToken)
    {
        IQueueConsumerService<ImageRecorderOnImagePushMessage> consumer = this;
        consumer.MessageReceived += subscriber;
        
        if (_imageRecorderConsumerTask == null)
        {
            ServiceBusProcessor processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions());
            _imageRecorderConsumerTask = Task.Factory.StartNew(async () =>
            {
                // add handler to process messages
                processor.ProcessMessageAsync += async (args) =>
                {
                    ServiceBusReceivedMessage message = args.Message;
                    _logger.LogInformation($"Received message: {message.Body}");
                    ImageRecorderMessageReceived?.Invoke(this, message.Body.ToObjectFromJson<ImageRecorderOnImagePushMessage>());
                    await args.CompleteMessageAsync(args.Message, cancellationToken);
                };

                // add handler to process any errors
                processor.ProcessErrorAsync += ErrorHandler;

                // start processing 
                await processor.StartProcessingAsync(cancellationToken);
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        

            await Task.CompletedTask;
        }
        
        
    }

    Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error when processing message");
        return Task.CompletedTask;
    }

    public async Task<bool> SentMessageToQueue(ImageRecorderOnImagePushMessage queueMessage, CancellationToken cancellationToken)
    {
        ServiceBusSender sender = _client.CreateSender(queueMessage.QueueName);
        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync(cancellationToken);
        try
        {
            string messageBody = ImageRecorderOnImagePushMessage.ToJson(queueMessage);
            ServiceBusMessage serviceBusMessage = new ServiceBusMessage(messageBody);
            bool canAddToBatch = messageBatch.TryAddMessage(serviceBusMessage);
            if(!canAddToBatch)
                throw new InvalidOperationException("Message is too large to fit in a batch");
            
            await sender.SendMessagesAsync(messageBatch, cancellationToken);
            return true;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    private event EventHandler<DetectionMessage>? DetectionMessageReceived;
    event EventHandler<DetectionMessage>? IQueueConsumerService<DetectionMessage>.MessageReceived
    {
        add {
            lock (_objectLock)
            {
                DetectionMessageReceived -= value;
                DetectionMessageReceived += value;
            }
        }
        remove
        {
            lock (_objectLock)
                DetectionMessageReceived -= value;
        }
    }

    public async Task GetMessageFromQueue(string queueName, EventHandler<DetectionMessage> subscriber, CancellationToken cancellationToken)
    {
        IQueueConsumerService<DetectionMessage> consumer = this;
        consumer.MessageReceived += subscriber;
        
        if (_detectionConsumerTask == null)
        {
            var processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions());
            _detectionConsumerTask = Task.Factory.StartNew(async () =>
            {
                // add handler to process messages
                processor.ProcessMessageAsync += async (args) =>
                {
                    ServiceBusReceivedMessage message = args.Message;
                    _logger.LogInformation($"Received message: {message.Body}");
                    DetectionMessageReceived?.Invoke(this, message.Body.ToObjectFromJson<DetectionMessage>());
                    await args.CompleteMessageAsync(args.Message, cancellationToken);
                };

                // add handler to process any errors
                processor.ProcessErrorAsync += ErrorHandler;

                // start processing 
                await processor.StartProcessingAsync(cancellationToken);
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        

            await Task.CompletedTask;
        }
        
    }

    public async Task<bool> SentMessageToQueue(DetectionMessage queueMessage, CancellationToken cancellationToken)
    {
        ServiceBusSender sender = _client.CreateSender(queueMessage.QueueName);
        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync(cancellationToken);
        try
        {
            string messageBody = DetectionMessage.ToJson(queueMessage);
            ServiceBusMessage serviceBusMessage = new ServiceBusMessage(messageBody);
            bool canAddToBatch = messageBatch.TryAddMessage(serviceBusMessage);
            if(!canAddToBatch)
                throw new InvalidOperationException("Message is too large to fit in a batch");
            
            await sender.SendMessagesAsync(messageBatch, cancellationToken);
            return true;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}
