using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SecurityCamera.Domain.InfrastructureServices;

namespace SecurityCamera.Infrastructure.RabbitMq;

public class RabbitMqService : IQueueConsumerService, IQueuePublisherService
{
    private readonly IConnection _connection;
    private readonly IModel _publisherChannel;
    private readonly IModel _consumerChannel;
    private readonly ILogger<RabbitMqService> _logger;

    public RabbitMqService(ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        string rabbitMQHostName = string.Empty;

        ConnectionFactory factory = new ConnectionFactory { HostName = rabbitMQHostName };
        _connection = factory.CreateConnection();
        _publisherChannel = _connection.CreateModel();
        _consumerChannel = _connection.CreateModel();
    }

    public async Task GetMessageFromQueue(string queueName, Action<QueueMessage> onMessageReceived, CancellationToken cancellationToken)
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
                var message = new QueueMessage
                {
                    QueueName = queueName,
                    Body = body,
                    QueueMessageHeaders = GetHeaderValues(basicDeliverEventArgs).ToArray()
                };
                onMessageReceived(message);
            };
            _consumerChannel.BasicConsume(queue: queueName,
                                autoAck: true,
                                consumer: consumer);
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        await Task.CompletedTask;
    }

    private IEnumerable<QueueMessageHeader> GetHeaderValues(BasicDeliverEventArgs basicDeliverEventArgs){
        var headers = basicDeliverEventArgs.BasicProperties.Headers;
        if(headers == null)
            yield break;
        foreach(var header in headers)
        {
            QueueMessageHeader queueMessageHeader = new QueueMessageHeader(header.Key, Encoding.UTF8.GetString((byte[])header.Value));
            yield return queueMessageHeader;
        }
            
    }

    public async Task<bool> SentMessageToQueue(QueueMessage queueMessage, CancellationToken cancellationToken)
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

            // Add headers to the properties
            if(queueMessage.QueueMessageHeaders != null)
            {
                var headers = new Dictionary<string, object>();
                foreach (var header in queueMessage.QueueMessageHeaders)
                    headers.Add(header.Key, header.Value);
                properties.Headers = headers;
            }
            

            _publisherChannel.BasicPublish(exchange: string.Empty,
                                routingKey: queueMessage.QueueName,
                                basicProperties: properties,
                                body: queueMessage.Body);

            _logger.LogInformation($"Sent Body size: {queueMessage.Body.Length}");
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
