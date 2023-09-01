using System.Diagnostics;
using System.Text;
using MetricsExample.Configuration;
using MetricsExample.Extensions;
using MetricsExample.Models;
using MetricsExample.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MetricsExample.Services;

public interface IProducer : IStartAsyncable
{
}

public class RabbitMqProducer : IProducer
{
    private readonly ExampleConfiguration _configuration;
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly Random _random = new();
    private IModel _channel;
    
    public RabbitMqProducer(IOptions<ExampleConfiguration> configuration, ILogger<RabbitMqProducer> logger)
    {
        _logger = logger;
        _configuration = configuration.Value;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Producer starting!");
        var factory = new ConnectionFactory { HostName = _configuration.RabbitHost, Port = _configuration.RabbitPort };
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();
        _channel.ExchangeDeclare(_configuration.RabbitExchange, "topic");
        _channel.QueueDeclare(queue: _configuration.RabbitOutputQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        _channel.QueueBind(_configuration.RabbitOutputQueue, _configuration.RabbitExchange, _configuration.RabbitRoutingKey);

        while (!stoppingToken.IsCancellationRequested)
        {
            ProduceFruit();
            await Task.Delay(_configuration.ProduceIntervalMillis, stoppingToken);
        }
    }

    private void ProduceFruit()
    {
        using var activity = Tracing.StartActivity(ActivityNames.ProducingFruit, ActivityKind.Producer);
        var properties = _channel.CreateBasicProperties();
        Tracing.SetActivityContext(activity, properties);
        
        var fruit = new Fruit(Fruit.KnownFruits[_random.Next(Fruit.KnownFruits.Length)], Guid.NewGuid());
        _logger.LogInformation("Producing a {fruitName}!", fruit.Name);
        
        var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fruit)));
        _channel.BasicPublish(_configuration.RabbitExchange, _configuration.RabbitRoutingKey, properties, body);

        activity?.SetTag("fruit", fruit.Name);
        Metrics.FruitsProduced.Add(1, fruit.GetLabels(_configuration.CardinalSin));
    }
}