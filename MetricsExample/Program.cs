// See https://aka.ms/new-console-template for more information

using MetricsExample.Configuration;
using MetricsExample.Observability;
using MetricsExample.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MetricsExample;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Action<OtlpExporterOptions> otlExporterOptionsAction = options =>
        {
            options.Protocol = OtlpExportProtocol.Grpc;
            options.Endpoint = new Uri("http://otelcol:4317");
        };
        
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(x =>
            {
                x.AddConsole();
                x.AddOpenTelemetry(builder =>
                {
                    builder.AddOtlpExporter(otlExporterOptionsAction);
                });
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOpenTelemetry()
                    .WithTracing(builder =>
                    {
                        builder
                            .AddOtlpExporter(otlExporterOptionsAction)
                            .AddSource(Constants.ServiceName);
                    })
                    .WithMetrics(builder =>
                    {
                        builder
                            .AddOtlpExporter(otlExporterOptionsAction)
                            .AddMeter(Metrics.Meter.Name);
                    })
                    .WithLogging()
                    .ConfigureResource(builder => builder.AddService(context.Configuration.GetValue<string>("Example:ServiceName") ?? Constants.ServiceName));
                
                services.Configure<ExampleConfiguration>(context.Configuration.GetSection(ExampleConfiguration.Key));
                services.AddSingleton<IConsumer, RabbitMqConsumer>();
                services.AddSingleton<IProducer, RabbitMqProducer>();
                services.AddSingleton<IForwarder, RabbitMqForwarder>();
                services.AddHostedService<Worker>();
            })
            .Build();

        await host.RunAsync();
    }
}