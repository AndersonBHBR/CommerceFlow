using Sales.Domain.Orders;

namespace Sales.Application.Orders;

public sealed record CreateOrderItemCommand(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    string ExternalReference,
    IReadOnlyCollection<CreateOrderItemCommand> Items,
    string CreatedBy,
    string Authorization,
    string CorrelationId);

public sealed record CancelOrderCommand(
    Guid OrderId,
    string RowVersion,
    string CancelledBy,
    string CorrelationId);

public sealed record CreateOrderResponse(OrderResponse Order, bool IsReplay);

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount)
{
    public static OrderItemResponse From(OrderItem item) => new(
        item.Id,
        item.ProductId,
        item.Sku,
        item.ProductName,
        item.Quantity,
        item.UnitPrice,
        item.TotalAmount);
}

public sealed record OrderResponse(
    Guid Id,
    string Number,
    Guid CustomerId,
    string ExternalReference,
    OrderStatus Status,
    string? StatusReason,
    decimal TotalAmount,
    string CreatedBy,
    DateTimeOffset CreatedAtUtc,
    string? CancelledBy,
    DateTimeOffset? CancelledAtUtc,
    string RowVersion,
    IReadOnlyList<OrderItemResponse> Items)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.Number,
        order.CustomerId,
        order.ExternalReference,
        order.Status,
        order.StatusReason,
        order.TotalAmount,
        order.CreatedBy,
        order.CreatedAtUtc,
        order.CancelledBy,
        order.CancelledAtUtc,
        Convert.ToBase64String(order.RowVersion),
        order.Items.Select(OrderItemResponse.From).ToArray());
}
