namespace Inventory.Domain.Stock;

public sealed class StockAdjustment
{
    private StockAdjustment()
    {
    }

    private StockAdjustment(
        Guid id,
        Guid productId,
        int quantity,
        string reason,
        string performedBy,
        string? externalReference,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
        Reason = reason;
        PerformedBy = performedBy;
        ExternalReference = externalReference;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string PerformedBy { get; private set; } = string.Empty;

    public string? ExternalReference { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static StockAdjustment Create(
        Guid productId,
        int quantity,
        string reason,
        string performedBy,
        string? externalReference,
        DateTimeOffset occurredAtUtc)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("O produto do ajuste é obrigatório.", nameof(productId));
        }

        if (quantity == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade do ajuste deve ser diferente de zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        if (normalizedReason.Length is < 3 or > 500)
        {
            throw new ArgumentException("O motivo deve possuir entre 3 e 500 caracteres.", nameof(reason));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(performedBy);
        var normalizedActor = performedBy.Trim();
        if (normalizedActor.Length > 200)
        {
            throw new ArgumentException("O responsável deve possuir no máximo 200 caracteres.", nameof(performedBy));
        }

        var normalizedReference = string.IsNullOrWhiteSpace(externalReference)
            ? null
            : externalReference.Trim();
        if (normalizedReference?.Length > 100)
        {
            throw new ArgumentException("A referência externa deve possuir no máximo 100 caracteres.", nameof(externalReference));
        }

        return new StockAdjustment(
            Guid.CreateVersion7(),
            productId,
            quantity,
            normalizedReason,
            normalizedActor,
            normalizedReference,
            occurredAtUtc);
    }
}
