using Inventory.Domain.Products;
using Inventory.Domain.Stock;

namespace Inventory.Application.Abstractions;

public interface IInventoryRepository
{
    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken);

    public Task<Product?> GetProductByIdAsync(Guid id, bool forUpdate, CancellationToken cancellationToken);

    public Task<Product?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken);

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> ListProductsAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    public Task<(IReadOnlyList<StockAdjustment> Items, int TotalCount)> ListAdjustmentsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    public void AddProduct(Product product);

    public void AddAdjustment(StockAdjustment adjustment);

    public void SetExpectedRowVersion(Product product, byte[] expectedRowVersion);

    public Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken);

    public void ClearTracking();
}

public enum SaveOutcome
{
    Success,
    DuplicateSku,
    ConcurrencyConflict
}
