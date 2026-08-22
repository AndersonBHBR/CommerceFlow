using Sales.Domain.Orders;

namespace Sales.Application.Abstractions;

public interface IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid orderId, bool forUpdate, CancellationToken cancellationToken);

    public Task<Order?> GetByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken);

    public Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? customerId,
        OrderStatus? status,
        CancellationToken cancellationToken);

    public void Add(Order order);

    public void SetExpectedRowVersion(Order order, byte[] expectedRowVersion);

    public Task<OrderSaveOutcome> SaveChangesAsync(CancellationToken cancellationToken);
}

public enum OrderSaveOutcome
{
    Success,
    DuplicateExternalReference,
    ConcurrencyConflict
}
