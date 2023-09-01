using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MetricsExample.Observability;

public static class Tracing
{
    private static readonly ActivitySource ActivitySource = new (Constants.ServiceName);
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static void SetActivityContext(Activity activity, IBasicProperties properties)
    {
        if (activity == null) return;
        
        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), properties, InjectContextIntoHeader);
        activity?.SetTag("messaging.system", "rabbitmq");
    }
    public static ActivityContext GetParentContext(BasicDeliverEventArgs eventArgs)
    {
        var context = Propagator.Extract(default, eventArgs.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = context.Baggage;
        return context.ActivityContext;
    }
    
    public static Activity StartActivity(string activityName, ActivityKind activityKind, ActivityContext? parentContext = null) =>
        parentContext != null 
            ? ActivitySource.StartActivity(activityName, activityKind, parentContext.Value) 
            : ActivitySource.StartActivity(activityName, activityKind);
    
    private static void InjectContextIntoHeader(IBasicProperties props, string key, string value)
    {
        props.Headers ??= new Dictionary<string, object>();
        props.Headers[key] = value;
    }
    
    private static IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        if (props.Headers.TryGetValue(key, out var value))
        {
            var bytes = value as byte[];
            return new[] { Encoding.UTF8.GetString(bytes) };
        }

        return Enumerable.Empty<string>();
    }
}

public static class ActivityNames
{
    public const string ConsumingFruit = "consume-fruit";
    public const string ProducingFruit = "produce-fruit";
    public const string ForwardingFruit = "forward-fruit";
}