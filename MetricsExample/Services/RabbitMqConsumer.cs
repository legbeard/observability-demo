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
using JsonException = System.Text.Json.JsonException;

namespace MetricsExample.Services;

public interface IStartAsyncable
{
    Task StartAsync(CancellationToken stoppingToken);
}
public interface IConsumer : IStartAsyncable
{
}

public class RabbitMqConsumer : IConsumer
{
    private readonly ExampleConfiguration _configuration;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IModel _channel;

    public RabbitMqConsumer(IOptions<ExampleConfiguration> configuration, ILogger<RabbitMqConsumer> logger)
    {
        _logger = logger;
        _configuration = configuration.Value;
    }
    
    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumer starting!");
        var factory = new ConnectionFactory { HostName = _configuration.RabbitHost, Port = _configuration.RabbitPort };
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();

        var consumer = new EventingBasicConsumer(_channel);
        
        connection.ConnectionShutdown += (_, eventArgs) => _logger.LogWarning("RabbitMq connection destroyed with cause: {0}", eventArgs.Cause);
        consumer.Received += async (sender, args) => await Consume(sender, args);

        _channel.BasicConsume(queue: _configuration.RabbitInputQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Consumer started!");
        return Task.CompletedTask;
    }

    private Task Consume(object sender, BasicDeliverEventArgs args)
    {
        var parentContext = Tracing.GetParentContext(args);

        using var activity = Tracing.StartActivity(ActivityNames.ConsumingFruit, ActivityKind.Consumer, parentContext);
        activity.SetTag("deliveryTag", args.DeliveryTag);

        if (!args.TryGet<Fruit>(out var fruit, out var error))
        {
            activity.SetStatus(ActivityStatusCode.Error, error);
            _logger.LogError("Rejecting message with delivery tag: {deliveryTag}, due to error: {error}", args.DeliveryTag, error);
            _channel.BasicReject(args.DeliveryTag, requeue: false);
            return Task.CompletedTask;
        }

        if (!Fruit.KnownFruits.Contains(fruit.Name))
        {
            activity.SetStatus(ActivityStatusCode.Error, $"Consumed a {fruit.Name}, which is not a known fruit!");
            _logger.LogWarning("Consumed an unknown fruit: {fruitName}", fruit.Name);
        }

        _logger.LogInformation("Received a {fruit}!", fruit.Name);
        
        Metrics.FruitsConsumed.Add(1, fruit.GetLabels(_configuration.CardinalSin));

        activity.SetTag("fruit", fruit.Name);
        activity.SetStatus(ActivityStatusCode.Ok, $"Successfully handled a {fruit.Name}!");
        _channel.BasicAck(args.DeliveryTag, multiple: false);

        return Task.CompletedTask;
    }
}