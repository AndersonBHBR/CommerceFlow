using CommerceFlow.Contracts.Messaging;

namespace CommerceFlow.Contracts.Events;

public sealed record OrderCreatedV1(
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<OrderItemV1> Items) : IIntegrationEvent;

public sealed record OrderItemV1(Guid ProductId, int Quantity);
