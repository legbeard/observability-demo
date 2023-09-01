namespace MetricsExample.Configuration;

public class ExampleConfiguration
{
    public const string Key = "Example";
    public string RabbitHost { get; set; }
    public int RabbitPort { get; set; }
    public string RabbitExchange { get; set; }
    public string RabbitInputQueue { get; set; }
    public string RabbitOutputQueue { get; set; }
    public string RabbitRoutingKey { get; set; }
    public string Mode { get; set; }
    public int ProduceIntervalMillis { get; set; } = 1000;
    public bool CardinalSin { get; set; }
}