using Sales.Domain.Orders;
using Xunit;

namespace CommerceFlow.UnitTests;

public sealed class OrderTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidItems_CalculatesTotalAndStartsPendingStock()
    {
        var order = CreateOrder([
            new OrderDraftItem(Guid.NewGuid(), "NOTE-001", "Notebook", 2, 3500m),
            new OrderDraftItem(Guid.NewGuid(), "MOUSE-001", "Mouse", 3, 100m)
        ]);

        Assert.Equal(OrderStatus.PendingStock, order.Status);
        Assert.Equal(7300m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
        Assert.StartsWith("CF-", order.Number);
    }

    [Fact]
    public void Create_WithDuplicateProduct_IsRejected()
    {
        var productId = Guid.NewGuid();
        var items = new[]
        {
            new OrderDraftItem(productId, "NOTE-001", "Notebook", 1, 3500m),
            new OrderDraftItem(productId, "NOTE-001", "Notebook", 1, 3500m)
        };

        Assert.Throws<ArgumentException>(() => CreateOrder(items));
    }

    [Fact]
    public void Cancel_WhenPendingStock_ChangesStatusAndKeepsAudit()
    {
        var order = CreateOrder([
            new OrderDraftItem(Guid.NewGuid(), "NOTE-001", "Notebook", 1, 3500m)
        ]);
        var cancelledAtUtc = CreatedAtUtc.AddMinutes(10);

        order.Cancel("sales-user", cancelledAtUtc);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("sales-user", order.CancelledBy);
        Assert.Equal(cancelledAtUtc, order.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_WhenConfirmed_RequestsCompensatingRelease()
    {
        var order = CreateOrder([
            new OrderDraftItem(Guid.NewGuid(), "NOTE-001", "Notebook", 1, 3500m)
        ]);
        order.ConfirmStock();

        order.Cancel("sales-user", CreatedAtUtc.AddMinutes(10));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Contains("liberação de estoque", order.StatusReason);
    }

    private static Order CreateOrder(IReadOnlyCollection<OrderDraftItem> items) =>
        Order.Create(
            Guid.CreateVersion7(),
            Guid.NewGuid(),
            "web-0001",
            "sales-user",
            CreatedAtUtc,
            items);
}
