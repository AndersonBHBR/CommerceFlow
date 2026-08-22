using CommerceFlow.Contracts.Messaging;

namespace CommerceFlow.Contracts.Events;

public sealed record StockReservedV1(
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    Guid OrderId,
    IReadOnlyCollection<ReservedStockItemV1> Items) : IIntegrationEvent;

public sealed record ReservedStockItemV1(Guid ProductId, int Quantity);
