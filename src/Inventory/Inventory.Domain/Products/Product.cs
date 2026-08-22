namespace Inventory.Domain.Products;

public sealed class Product
{
    private Product()
    {
    }

    private Product(Guid id, string sku, string name, int initialQuantity)
    {
        Id = id;
        Sku = sku;
        Name = name;
        IsActive = true;
        AvailableQuantity = initialQuantity;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int AvailableQuantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public int FreeQuantity => AvailableQuantity - ReservedQuantity;

    public static Product Create(string sku, string name, int initialQuantity)
    {
        if (initialQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialQuantity), "O saldo inicial não pode ser negativo.");
        }

        return new Product(
            Guid.CreateVersion7(),
            NormalizeSku(sku),
            NormalizeName(name),
            initialQuantity);
    }

    public static string NormalizeSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        var normalized = sku.Trim().ToUpperInvariant();
        if (normalized.Length is < 3 or > 64)
        {
            throw new ArgumentException("O SKU deve possuir entre 3 e 64 caracteres.", nameof(sku));
        }

        if (!normalized.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'))
        {
            throw new ArgumentException(
                "O SKU pode conter somente letras, números, hífen, sublinhado e ponto.",
                nameof(sku));
        }

        return normalized;
    }

    public void Update(string name, bool isActive)
    {
        Name = NormalizeName(name);
        IsActive = isActive;
    }

    public void AdjustStock(int quantity)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Não é possível ajustar estoque de produto inativo.");
        }

        if (quantity == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade do ajuste deve ser diferente de zero.");
        }

        var adjustedQuantity = (long)AvailableQuantity + quantity;
        if (adjustedQuantity is > int.MaxValue or < int.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "O ajuste excede o limite permitido.");
        }

        if (adjustedQuantity < ReservedQuantity)
        {
            throw new InvalidOperationException("Saldo livre insuficiente para realizar a saída de estoque.");
        }

        AvailableQuantity = (int)adjustedQuantity;
    }

    public void ReserveStock(int quantity)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Não é possível reservar estoque de produto inativo.");
        }

        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade reservada deve ser positiva.");
        }

        if (FreeQuantity < quantity)
        {
            throw new InvalidOperationException("Saldo livre insuficiente para realizar a reserva.");
        }

        ReservedQuantity += quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade liberada deve ser positiva.");
        }

        if (ReservedQuantity < quantity)
        {
            throw new InvalidOperationException("A quantidade liberada excede o saldo reservado.");
        }

        ReservedQuantity -= quantity;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = name.Trim();
        if (normalized.Length is < 2 or > 200)
        {
            throw new ArgumentException("O nome deve possuir entre 2 e 200 caracteres.", nameof(name));
        }

        return normalized;
    }
}
