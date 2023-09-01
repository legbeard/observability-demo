using MetricsExample.Configuration;
using MetricsExample.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MetricsExample;

public class Worker : BackgroundService
{

    private readonly ILogger<Worker> _logger;
    private readonly IConsumer _consumer;
    private readonly IProducer _producer;
    private readonly IForwarder _forwarder;
    private readonly ExampleConfiguration _configuration;
    
    public Worker(IConsumer consumer, IProducer producer, IForwarder forwarder, ILogger<Worker> logger, IOptions<ExampleConfiguration> configuration)
    {
        _consumer = consumer;
        _producer = producer;
        _logger = logger;
        _forwarder = forwarder;
        _configuration = configuration.Value;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        switch (_configuration.Mode)
        {
            case "consumer":
                await _consumer.StartAsync(stoppingToken);
                break;
            case "producer":
                await _producer.StartAsync(stoppingToken);
                break;
            case "forwarder":
                await _forwarder.StartAsync(stoppingToken);
                break;
            default:
                throw new ArgumentException($"Mode is set to '{_configuration.Mode}', which is unknown");
        }
    }
}