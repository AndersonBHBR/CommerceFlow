namespace Sales.Application.Abstractions;

public interface IInventoryCatalog
{
    public Task<InventoryLookupResult> GetProductAsync(
        Guid productId,
        string authorization,
        CancellationToken cancellationToken);
}

public sealed record InventoryProductSnapshot(
    Guid Id,
    string Sku,
    string Name,
    bool IsActive,
    int FreeQuantity);

public sealed record InventoryLookupResult(
    InventoryLookupStatus Status,
    InventoryProductSnapshot? Product,
    string? Detail);

public enum InventoryLookupStatus
{
    Found,
    NotFound,
    Unavailable
}
