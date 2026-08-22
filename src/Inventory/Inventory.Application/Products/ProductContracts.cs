using Inventory.Domain.Products;
using Inventory.Domain.Stock;

namespace Inventory.Application.Products;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    int InitialQuantity,
    string PerformedBy);

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    bool IsActive,
    string RowVersion);

public sealed record AdjustStockCommand(
    Guid ProductId,
    int Quantity,
    string Reason,
    string? ExternalReference,
    string PerformedBy);

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    bool IsActive,
    int AvailableQuantity,
    int ReservedQuantity,
    int FreeQuantity,
    string RowVersion)
{
    public static ProductResponse From(Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.IsActive,
        product.AvailableQuantity,
        product.ReservedQuantity,
        product.FreeQuantity,
        Convert.ToBase64String(product.RowVersion));
}

public sealed record StockAdjustmentResponse(
    Guid Id,
    Guid ProductId,
    int Quantity,
    string Reason,
    string PerformedBy,
    string? ExternalReference,
    DateTimeOffset OccurredAtUtc)
{
    public static StockAdjustmentResponse From(StockAdjustment adjustment) => new(
        adjustment.Id,
        adjustment.ProductId,
        adjustment.Quantity,
        adjustment.Reason,
        adjustment.PerformedBy,
        adjustment.ExternalReference,
        adjustment.OccurredAtUtc);
}

public sealed record StockAdjustmentCreatedResponse(
    StockAdjustmentResponse Adjustment,
    ProductResponse Product);
