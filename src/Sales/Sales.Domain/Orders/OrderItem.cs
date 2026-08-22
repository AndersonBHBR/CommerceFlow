namespace Sales.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    private OrderItem(
        Guid id,
        Guid orderId,
        Guid productId,
        string sku,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        Sku = sku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalAmount = quantity * unitPrice;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TotalAmount { get; private set; }

    public static OrderItem Create(
        Guid orderId,
        Guid productId,
        string sku,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("O pedido do item é obrigatório.", nameof(orderId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("O produto do item é obrigatório.", nameof(productId));
        }

        if (quantity is < 1 or > 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade deve ficar entre 1 e 100.000.");
        }

        if (unitPrice is < 0.01m or > 1_000_000m)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "O preço unitário deve ficar entre 0,01 e 1.000.000,00.");
        }

        return new OrderItem(
            Guid.CreateVersion7(),
            orderId,
            productId,
            NormalizeRequired(sku, 64, "SKU"),
            NormalizeRequired(productName, 200, "nome do produto"),
            quantity,
            decimal.Round(unitPrice, 2, MidpointRounding.ToEven));
    }

    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"O {fieldName} deve possuir no máximo {maximumLength} caracteres.", nameof(value));
        }

        return normalized;
    }
}
