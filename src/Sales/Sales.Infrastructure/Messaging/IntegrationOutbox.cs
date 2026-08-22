using System.Text.Json;
using CommerceFlow.Contracts.Messaging;
using Sales.Application.Abstractions;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Persistence.Messaging;

namespace Sales.Infrastructure.Messaging;

public sealed class IntegrationOutbox(SalesDbContext dbContext) : IIntegrationOutbox
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Enqueue<T>(T message, string routingKey) where T : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        var type = typeof(T).FullName ?? typeof(T).Name;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            message.MessageId,
            message.OccurredAtUtc,
            message.CorrelationId,
            type,
            routingKey,
            JsonSerializer.Serialize(message, SerializerOptions)));
    }
}
