using RabbitMQ.Client;

string rabbitMQHostName = GetFromArgs(args, "--rabbitMqHostName");
string queueName = GetFromArgs(args, "--queueName");
string imagesDirPath = GetFromArgs(args, "--imagesDirPath");
string cameraName = GetFromArgs(args, "--cameraName");
string routingKey = GetFromArgs(args, "--routingKey");

PushToRabbitMQ((channel) => CheckForImageAndPublish(channel, imagesDirPath, cameraName, routingKey), rabbitMQHostName, queueName);

static string GetFromArgs(string[] args, string key)
{
    string? value = args.FirstOrDefault(arg => arg.StartsWith($"{key}="))?.Split('=')[1];
    if(string.IsNullOrEmpty(value)) throw new ArgumentException($"{key} not provided");
    return value;
}


static void PushToRabbitMQ(Action<IModel> action, string rabbitMQHostName, string queueName)
{
    var factory = new ConnectionFactory { HostName = rabbitMQHostName };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();
    channel.QueueDeclare(queue: queueName,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

    action(channel);
}

static void CheckForImageAndPublish(IModel channel, string imagesDirPath, string cameraName, string routingKey)
{
    if(Directory.Exists(imagesDirPath) == false) throw new ArgumentException("Image Path does not exist");

    while (true)
    {
        string[] imagesPaths = 
            Directory.GetFiles(imagesDirPath, "*.jpg")
            .Union(Directory.GetFiles(imagesDirPath, "*.png"))
            .ToArray();
        
        Array.ForEach(imagesPaths, (imagePath) =>
        {
            byte[] body = File.ReadAllBytes(imagePath);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            // Add headers to the properties
            properties.Headers = new Dictionary<string, object>
            {
                { "CreatedDateTimeUTC", new FileInfo(imagePath).CreationTimeUtc.ToString() },
                { "Camera", cameraName }
            };

            channel.BasicPublish(exchange: string.Empty,
                                routingKey: routingKey,
                                basicProperties: properties,
                                body: body);

            Console.WriteLine($" {imagePath} Sent Body size: {body.Length}");

            // Delete the file after publishing
            File.Delete(imagePath);
        });
        
        Thread.Sleep(1000);
    }
}
