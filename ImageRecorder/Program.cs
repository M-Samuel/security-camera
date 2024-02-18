using System.Text;
using RabbitMQ.Client;
using System.IO;


if (args.Length != 1)
    throw new ArgumentException("Image Path not provided");

string imagePath = args[0];

if (!File.Exists(imagePath))
    throw new ArgumentException($"Image not found at {imagePath}");


var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "CameraImages",
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);


byte[] body = File.ReadAllBytes(imagePath);


var properties = channel.CreateBasicProperties();
properties.Persistent = true;

// Add headers to the properties
properties.Headers = new Dictionary<string, object>
{
    { "DateTimeUTC", DateTime.UtcNow.ToString() },
    { "Camera", "FrontCamera" }
};


channel.BasicPublish(exchange: string.Empty,
                     routingKey: "CameraImages",
                     basicProperties: properties,
                     body: body);

Console.WriteLine($" [x] Sent Body size: {body.Length}");
