using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence;

public sealed class OrderRepository(SalesDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid orderId, bool forUpdate, CancellationToken cancellationToken)
    {
        var query = dbContext.Orders
            .Include(order => order.Items)
            .AsQueryable();

        if (!forUpdate)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task<Order?> GetByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken) =>
        dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(
                order => order.ExternalReference == externalReference,
                cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? customerId,
        OrderStatus? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders.AsNoTracking();
        if (customerId is not null)
        {
            query = query.Where(order => order.CustomerId == customerId.Value);
        }

        if (status is not null)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(Order order) => dbContext.Orders.Add(order);

    public void SetExpectedRowVersion(Order order, byte[] expectedRowVersion) =>
        dbContext.Entry(order).Property(item => item.RowVersion).OriginalValue = expectedRowVersion;

    public async Task<OrderSaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var hasNewOrder = dbContext.ChangeTracker
            .Entries<Order>()
            .Any(entry => entry.State == EntityState.Added);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return OrderSaveOutcome.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return OrderSaveOutcome.ConcurrencyConflict;
        }
        catch (DbUpdateException) when (hasNewOrder)
        {
            return OrderSaveOutcome.DuplicateExternalReference;
        }
    }
}
