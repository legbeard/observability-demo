using System.Diagnostics;
using System.Text;
using MetricsExample.Models;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace MetricsExample.Extensions;

public static class BasicDeliverEventArgsExtensions
{
    public static bool TryGet<T>(this BasicDeliverEventArgs eventArgs, out T message, out string error) where T : class
    {
        error = null;
        message = null;
        
        var body = eventArgs.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Payload was empty";
            return false;
        }

        try
        {
            var jsonSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };
            message = JsonConvert.DeserializeObject<T>(json, jsonSettings);
            return true;
        }
        catch (JsonException _)
        {
            error = $"Unable to deserialize the received payload to type '{typeof(T).FullName}, received payload: {json}";
            return false;
        }
    }
}