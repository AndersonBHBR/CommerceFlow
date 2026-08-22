using CommerceFlow.Contracts.Messaging;

namespace CommerceFlow.Contracts.Events;

public sealed record StockRejectedV1(
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    Guid OrderId,
    StockRejectionReasonV1 ReasonCode,
    IReadOnlyCollection<RejectedStockItemV1> Items) : IIntegrationEvent;

public sealed record RejectedStockItemV1(Guid ProductId, int Quantity, string Detail);

public enum StockRejectionReasonV1
{
    ProductNotFound = 1,
    ProductInactive = 2,
    InsufficientStock = 3
}
