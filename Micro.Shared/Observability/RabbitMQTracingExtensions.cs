using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Micro.Shared.Observability;

public static class RabbitMQTracingExtensions
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    /// <summary>
    /// Injects the current Activity context (TraceId, SpanId) into the RabbitMQ BasicProperties headers.
    /// Call this immediately before channel.BasicPublish.
    /// </summary>
    public static void InjectTraceContext(this IBasicProperties properties)
    {
        properties.Headers ??= new Dictionary<string, object>();
        
        var activity = Activity.Current;
        if (activity != null)
        {
            Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), properties, InjectTraceContextIntoHeaders);
        }
    }

    private static void InjectTraceContextIntoHeaders(IBasicProperties props, string key, string value)
    {
        try
        {
            props.Headers ??= new Dictionary<string, object>();
            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to inject trace context: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts the trace context from RabbitMQ BasicProperties headers.
    /// Use this to extract context when receiving a message.
    /// </summary>
    public static PropagationContext ExtractTraceContext(this IBasicProperties properties)
    {
        return Propagator.Extract(default, properties, ExtractTraceContextFromHeaders);
    }

    private static IEnumerable<string> ExtractTraceContextFromHeaders(IBasicProperties props, string key)
    {
        try
        {
            if (props.Headers != null && props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                if (bytes != null)
                {
                    return new[] { Encoding.UTF8.GetString(bytes) };
                }
                var str = value as string;
                if (str != null)
                {
                    return new[] { str };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to extract trace context: {ex.Message}");
        }

        return Enumerable.Empty<string>();
    }
}
