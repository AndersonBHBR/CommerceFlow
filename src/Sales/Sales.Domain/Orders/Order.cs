namespace Sales.Domain.Orders;

public sealed record OrderDraftItem(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    private Order(
        Guid id,
        Guid customerId,
        string externalReference,
        string createdBy,
        DateTimeOffset createdAtUtc,
        IReadOnlyCollection<OrderDraftItem> items)
    {
        Id = id;
        Number = $"CF-{id:N}".ToUpperInvariant();
        CustomerId = customerId;
        ExternalReference = externalReference;
        Status = OrderStatus.PendingStock;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();

        _items.AddRange(items.Select(item => OrderItem.Create(
            id,
            item.ProductId,
            item.Sku,
            item.ProductName,
            item.Quantity,
            item.UnitPrice)));
        TotalAmount = _items.Sum(item => item.TotalAmount);
    }

    public Guid Id { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public string ExternalReference { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public string? StatusReason { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? CancelledBy { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(
        Guid id,
        Guid customerId,
        string externalReference,
        string createdBy,
        DateTimeOffset createdAtUtc,
        IReadOnlyCollection<OrderDraftItem> items)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("O identificador do pedido é obrigatório.", nameof(id));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("O cliente é obrigatório.", nameof(customerId));
        }

        ArgumentNullException.ThrowIfNull(items);
        if (items.Count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(items), "O pedido deve possuir entre 1 e 100 itens.");
        }

        if (items.Select(item => item.ProductId).Distinct().Count() != items.Count)
        {
            throw new ArgumentException("O mesmo produto não pode aparecer mais de uma vez no pedido.", nameof(items));
        }

        return new Order(
            id,
            customerId,
            NormalizeExternalReference(externalReference),
            NormalizeActor(createdBy),
            createdAtUtc,
            items);
    }

    public void ConfirmStock()
    {
        EnsurePendingStock();
        Status = OrderStatus.Confirmed;
        StatusReason = null;
    }

    public void RejectStock(string reason)
    {
        EnsurePendingStock();
        Status = OrderStatus.Rejected;
        StatusReason = NormalizeReason(reason);
    }

    public void Cancel(string cancelledBy, DateTimeOffset cancelledAtUtc)
    {
        if (Status is not (OrderStatus.PendingStock or OrderStatus.Confirmed))
        {
            throw new InvalidOperationException("Somente pedidos aguardando estoque ou confirmados podem ser cancelados.");
        }

        var previousStatus = Status;
        Status = OrderStatus.Cancelled;
        StatusReason = previousStatus == OrderStatus.Confirmed
            ? "Cancelado após a reserva; liberação de estoque solicitada."
            : "Cancelado enquanto aguardava a reserva de estoque.";
        CancelledBy = NormalizeActor(cancelledBy);
        CancelledAtUtc = cancelledAtUtc.ToUniversalTime();
    }

    public static string NormalizeExternalReference(string externalReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalReference);
        var normalized = externalReference.Trim().ToUpperInvariant();
        if (normalized.Length is < 3 or > 100)
        {
            throw new ArgumentException(
                "A referência externa deve possuir entre 3 e 100 caracteres.",
                nameof(externalReference));
        }

        return normalized;
    }

    private void EnsurePendingStock()
    {
        if (Status != OrderStatus.PendingStock)
        {
            throw new InvalidOperationException("Somente pedidos aguardando estoque podem realizar esta transição.");
        }
    }

    private static string NormalizeActor(string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        var normalized = actor.Trim();
        if (normalized.Length > 200)
        {
            throw new ArgumentException("O responsável deve possuir no máximo 200 caracteres.", nameof(actor));
        }

        return normalized;
    }

    private static string NormalizeReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalized = reason.Trim();
        if (normalized.Length > 500)
        {
            throw new ArgumentException("O motivo deve possuir no máximo 500 caracteres.", nameof(reason));
        }

        return normalized;
    }
}
