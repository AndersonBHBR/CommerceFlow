using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Inventory.Infrastructure.Messaging;

internal static class MessagingTelemetry
{
    private const string InstrumentationName = "CommerceFlow.Inventory.Messaging";
    private static readonly ActivitySource ActivitySource = new(InstrumentationName);
    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly Counter<long> PublishedCounter = Meter.CreateCounter<long>("commerceflow.messaging.published");
    private static readonly Counter<long> ProcessedCounter = Meter.CreateCounter<long>("commerceflow.messaging.processed");
    private static readonly Counter<long> FailedCounter = Meter.CreateCounter<long>("commerceflow.messaging.failed");

    public static Activity? StartPublish(string routingKey, string correlationId)
    {
        var activity = ActivitySource.StartActivity("rabbitmq publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.name", "publish");
        activity?.SetTag("messaging.destination.name", routingKey);
        activity?.SetTag("commerceflow.correlation_id", correlationId);
        return activity;
    }

    public static Activity? StartConsume(string routingKey, string? correlationId)
    {
        var activity = ActivitySource.StartActivity("rabbitmq process", ActivityKind.Consumer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.name", "process");
        activity?.SetTag("messaging.destination.name", routingKey);
        activity?.SetTag("commerceflow.correlation_id", correlationId);
        return activity;
    }

    public static void RecordPublished(string routingKey) =>
        PublishedCounter.Add(1, new KeyValuePair<string, object?>("messaging.destination.name", routingKey));

    public static void RecordProcessed(string routingKey) =>
        ProcessedCounter.Add(1, new KeyValuePair<string, object?>("messaging.destination.name", routingKey));

    public static void RecordFailed(string routingKey) =>
        FailedCounter.Add(1, new KeyValuePair<string, object?>("messaging.destination.name", routingKey));
}
