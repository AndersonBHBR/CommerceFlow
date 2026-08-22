namespace Inventory.Domain.Stock;

public sealed class StockReservation
{
    private StockReservation()
    {
    }

    private StockReservation(Guid orderId, Guid productId, int quantity, DateTimeOffset reservedAtUtc)
    {
        Id = Guid.CreateVersion7();
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        ReservedAtUtc = reservedAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset ReservedAtUtc { get; private set; }

    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    public static StockReservation Create(
        Guid orderId,
        Guid productId,
        int quantity,
        DateTimeOffset reservedAtUtc)
    {
        if (orderId == Guid.Empty || productId == Guid.Empty)
        {
            throw new ArgumentException("Pedido e produto são obrigatórios para reservar estoque.");
        }

        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade reservada deve ser positiva.");
        }

        return new StockReservation(orderId, productId, quantity, reservedAtUtc);
    }

    public void Release(DateTimeOffset releasedAtUtc)
    {
        if (ReleasedAtUtc is not null)
        {
            return;
        }

        ReleasedAtUtc = releasedAtUtc.ToUniversalTime();
    }
}
