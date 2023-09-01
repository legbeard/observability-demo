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
using RabbitMQ.Client.Events;

namespace MetricsExample.Services;

public interface IForwarder : IStartAsyncable
{
}

public class RabbitMqForwarder : IForwarder
{
    private readonly ExampleConfiguration _configuration;
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly Random _random = new();
    private IModel _channel;

    public RabbitMqForwarder(IOptions<ExampleConfiguration> configuration, ILogger<RabbitMqProducer> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumer starting!");
        var factory = new ConnectionFactory { HostName = _configuration.RabbitHost, Port = _configuration.RabbitPort };
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();
        _channel.ExchangeDeclare(_configuration.RabbitExchange, "topic");
        _channel.QueueDeclare(queue: _configuration.RabbitOutputQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        _channel.QueueBind(_configuration.RabbitOutputQueue, _configuration.RabbitExchange, _configuration.RabbitRoutingKey);

        var consumer = new EventingBasicConsumer(_channel);
        
        connection.ConnectionShutdown += (_, eventArgs) => _logger.LogWarning("RabbitMq connection destroyed with cause: {0}", eventArgs.Cause);
        consumer.Received += async (sender, args) => await Forward(sender, args);

        _channel.BasicConsume(queue: _configuration.RabbitInputQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Consumer started!");
        return Task.CompletedTask;
    }

    private Task Forward(object sender, BasicDeliverEventArgs args)
    {
        var parentContext = Tracing.GetParentContext(args);
        using var consumeActivity = Tracing.StartActivity(ActivityNames.ForwardingFruit, ActivityKind.Consumer, parentContext);

        consumeActivity.SetTag("delivery_tag", args.DeliveryTag);

        if (!args.TryGet<Fruit>(out var fruit, out var error))
        {
            consumeActivity.SetStatus(ActivityStatusCode.Error, error);
            _logger.LogError("Rejecting message with delivery tag: {deliveryTag}, due to error: {error}", args.DeliveryTag, error);
            _channel.BasicReject(args.DeliveryTag, requeue: false);
            return Task.CompletedTask;
        }

        consumeActivity.SetTag("fruit", fruit.Name).SetStatus(ActivityStatusCode.Ok, $"Successfully received a {fruit.Name}");
        
        _logger.LogInformation("Forwarding a {fruit}!", fruit.Name);
        
        using var produceActivity = Tracing.StartActivity(ActivityNames.ForwardingFruit, ActivityKind.Producer, consumeActivity.Context);
        
        var fruitMessage = _random.Next(1000) switch
        {
            // Rarely the forwarder does a terrible job forwards garbage instead of a fruit, but thinks everything is fine
            < 1 => JsonConvert.SerializeObject(new Garbage(_random.NextDouble() * 20, "A pile of yellow socks", "Sour")),
            // Sometimes it thinks dogs are fruit
            < 4 => JsonConvert.SerializeObject(fruit with { Name = "Dog" }),
            // The rest of the time, it simply forwards the fruit
            _ => JsonConvert.SerializeObject(fruit)
        };

        var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(fruitMessage));
        _channel.BasicPublish(_configuration.RabbitExchange, _configuration.RabbitRoutingKey, args.BasicProperties, body);
        
        produceActivity.SetTag("fruit", fruit.Name).SetStatus(ActivityStatusCode.Ok, $"Successfully forwarded a {fruit.Name}!");
        _channel.BasicAck(args.DeliveryTag, multiple: false);
        
        Metrics.FruitsForwarded.Add(1, fruit.GetLabels(_configuration.CardinalSin));

        return Task.CompletedTask;
    }
}