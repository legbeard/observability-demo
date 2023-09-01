using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

Action<OtlpExporterOptions> otlExporterOptionsAction = options =>
{
    options.Protocol = OtlpExportProtocol.Grpc;
    options.Endpoint = new Uri("http://otelcol:4317");
};

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(x => x.AddOtlpExporter(otlExporterOptionsAction));
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddAspNetCoreInstrumentation().AddOtlpExporter(otlExporterOptionsAction))
    .WithMetrics(b => b.AddAspNetCoreInstrumentation().AddOtlpExporter(otlExporterOptionsAction))
    .WithLogging()
    .ConfigureResource(b => b.AddService("api"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/hello", () => $"Hello World!");
app.MapGet("/api/hello/{id}", (string id) => $"Hello {id}!");
app.MapGet("/api/bad", () => DateTimeOffset.Now.ToUnixTimeMilliseconds() % 2 == 1 ? "It wasn't even bad" : throw new NotImplementedException("Confirmed to be bad"));

app.Run();