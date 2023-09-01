using MetricsExample.Models;

namespace MetricsExample.Extensions;

public static class FruitExtensions
{
    public static KeyValuePair<string, object>[] GetLabels(this Fruit fruit, bool sin)
    {
        return sin ? new []{ new KeyValuePair<string, object>("type", fruit.Name), new KeyValuePair<string, object>("id", fruit.Guid)} 
                   : new []{ new KeyValuePair<string, object>("type", fruit.Name)};
    }
}