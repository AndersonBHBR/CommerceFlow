using Inventory.Application.Abstractions;
using Inventory.Domain.Products;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(product => product.Sku == sku, cancellationToken);

    public Task<Product?> GetProductByIdAsync(
        Guid id,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        var query = forUpdate
            ? dbContext.Products.AsQueryable()
            : dbContext.Products.AsNoTracking();

        return query.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public Task<Product?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken) =>
        dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Sku == sku, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListProductsAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(product =>
                product.Sku.Contains(search) || product.Name.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<StockAdjustment> Items, int TotalCount)> ListAdjustmentsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockAdjustments
            .AsNoTracking()
            .Where(adjustment => adjustment.ProductId == productId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(adjustment => adjustment.OccurredAtUtc)
            .ThenByDescending(adjustment => adjustment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public void AddProduct(Product product) => dbContext.Products.Add(product);

    public void AddAdjustment(StockAdjustment adjustment) => dbContext.StockAdjustments.Add(adjustment);

    public void SetExpectedRowVersion(Product product, byte[] expectedRowVersion) =>
        dbContext.Entry(product).Property(item => item.RowVersion).OriginalValue = expectedRowVersion;

    public async Task<SaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var hasNewProduct = dbContext.ChangeTracker
            .Entries<Product>()
            .Any(entry => entry.State == EntityState.Added);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return SaveOutcome.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SaveOutcome.ConcurrencyConflict;
        }
        catch (DbUpdateException) when (hasNewProduct)
        {
            return SaveOutcome.DuplicateSku;
        }
    }

    public void ClearTracking() => dbContext.ChangeTracker.Clear();
}
