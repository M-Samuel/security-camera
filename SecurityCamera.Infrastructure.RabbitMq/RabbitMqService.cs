using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SecurityCamera.Domain.ImageRecorderDomain;
using SecurityCamera.Domain.InfrastructureServices;
using SecurityCamera.Domain.ObjectDetectionDomain;

namespace SecurityCamera.Infrastructure.RabbitMq;

public class RabbitMqService : 
IQueueConsumerService<ImageRecorderOnImagePushMessage>, 
IQueuePublisherService<ImageRecorderOnImagePushMessage>,
IQueueConsumerService<DetectionMessage>, 
IQueuePublisherService<DetectionMessage>
{
    private readonly IConnection _connection;
    private readonly IModel _publisherChannel;
    private readonly IModel _consumerChannel;
    private readonly ILogger<RabbitMqService> _logger;

    public RabbitMqService(ILogger<RabbitMqService> logger, IConfiguration configuration)
    {
        _logger = logger;

        ConnectionFactory factory = new ConnectionFactory { HostName = configuration[nameof(Args.RabbitMqHostName)] };
        _connection = factory.CreateConnection();
        _publisherChannel = _connection.CreateModel();
        _consumerChannel = _connection.CreateModel();
    }

    public async Task GetMessageFromQueue(string queueName, Action<ImageRecorderOnImagePushMessage> onMessageReceived, CancellationToken cancellationToken)
    {
        _consumerChannel.QueueDeclare(queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

        Task consumerTask = Task.Factory.StartNew(() =>
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += (model, basicDeliverEventArgs) =>
            {
                _logger.LogInformation($"Message Received from the queue {queueName} {basicDeliverEventArgs.DeliveryTag}");
                byte[] body = basicDeliverEventArgs.Body.ToArray();
                var message = QueueMessage.FromByteArray<ImageRecorderOnImagePushMessage>(body);
                if(message != null)
                    onMessageReceived(message);
            };
            _consumerChannel.BasicConsume(queue: queueName,
                                autoAck: true,
                                consumer: consumer);
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        await Task.CompletedTask;
    }

    public async Task<bool> SentMessageToQueue(ImageRecorderOnImagePushMessage queueMessage, CancellationToken cancellationToken)
    {
        try
        {
            _publisherChannel.QueueDeclare(queue: queueMessage.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
        
            IBasicProperties properties = _publisherChannel.CreateBasicProperties();
            properties.Persistent = true;

            byte[] messageBodyBytes = queueMessage.ToByteArray();
            _publisherChannel.BasicPublish(exchange: string.Empty,
                                routingKey: queueMessage.QueueName,
                                basicProperties: properties,
                                body: messageBodyBytes);

            _logger.LogInformation($"Sent Body size: {messageBodyBytes.Length}");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error: {ex.Message}");
            return await Task.FromResult(false);
        }
        
    }

    public async Task GetMessageFromQueue(string queueName, Action<DetectionMessage> onMessageReceived, CancellationToken cancellationToken)
    {
        _consumerChannel.QueueDeclare(queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

        Task consumerTask = Task.Factory.StartNew(() =>
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += (model, basicDeliverEventArgs) =>
            {
                _logger.LogInformation($"Message Received from the queue {queueName} {basicDeliverEventArgs.DeliveryTag}");
                byte[] body = basicDeliverEventArgs.Body.ToArray();
                var message = QueueMessage.FromByteArray<DetectionMessage>(body);
                if(message != null)
                    onMessageReceived(message);
            };
            _consumerChannel.BasicConsume(queue: queueName,
                                autoAck: true,
                                consumer: consumer);
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        await Task.CompletedTask;
    }

    public async Task<bool> SentMessageToQueue(DetectionMessage queueMessage, CancellationToken cancellationToken)
    {
        try
        {
            _publisherChannel.QueueDeclare(queue: queueMessage.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
        
            IBasicProperties properties = _publisherChannel.CreateBasicProperties();
            properties.Persistent = true;

            byte[] messageBodyBytes = queueMessage.ToByteArray();
            _publisherChannel.BasicPublish(exchange: string.Empty,
                                routingKey: queueMessage.QueueName,
                                basicProperties: properties,
                                body: messageBodyBytes);

            _logger.LogInformation($"Sent Body size: {messageBodyBytes.Length}");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error: {ex.Message}");
            return await Task.FromResult(false);
        }
        
    }

    public void Dispose()
    {
        _consumerChannel.Dispose();
        _publisherChannel.Dispose();
        _connection.Dispose();
    }
}
