using System.Diagnostics.Metrics;

namespace MetricsExample.Observability;

public class Metrics
{
    public static readonly Meter Meter = new (Constants.ServiceName);

    private const string FruitsProducedName = "fruit_produced";
    private const string FruitsConsumedName = "fruit_consumed";
    private const string FruitsForwardedName = "fruit_forwarded";
    
    public static readonly Counter<long> FruitsProduced = Meter.CreateCounter<long>(FruitsProducedName);
    public static readonly Counter<long> FruitsConsumed = Meter.CreateCounter<long>(FruitsConsumedName);
    public static readonly Counter<long> FruitsForwarded = Meter.CreateCounter<long>(FruitsForwardedName);
}