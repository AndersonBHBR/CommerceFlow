using CommerceFlow.Contracts.Messaging;

namespace CommerceFlow.Contracts.Events;

public sealed record StockReleaseRequestedV1(
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    Guid OrderId) : IIntegrationEvent;
