using Inventory.Domain.Products;
using Xunit;

namespace CommerceFlow.UnitTests;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidData_NormalizesSkuAndStartsActive()
    {
        var product = Product.Create("  sku-001  ", "Produto de teste", 10);

        Assert.Equal("SKU-001", product.Sku);
        Assert.Equal("Produto de teste", product.Name);
        Assert.True(product.IsActive);
        Assert.Equal(10, product.AvailableQuantity);
        Assert.Equal(0, product.ReservedQuantity);
        Assert.Equal(10, product.FreeQuantity);
    }

    [Fact]
    public void Create_WithNegativeInitialQuantity_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create("SKU-001", "Produto de teste", -1));
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("SKU COM ESPAÇO")]
    [InlineData("SKU/INVALIDO")]
    public void Create_WithInvalidSku_IsRejected(string sku)
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create(sku, "Produto de teste", 0));
    }

    [Fact]
    public void AdjustStock_WithEntry_UpdatesAvailableAndFreeQuantities()
    {
        var product = Product.Create("SKU-001", "Produto de teste", 10);

        product.AdjustStock(5);

        Assert.Equal(15, product.AvailableQuantity);
        Assert.Equal(15, product.FreeQuantity);
    }

    [Fact]
    public void AdjustStock_WithOutputBeyondFreeQuantity_IsRejectedWithoutChangingBalance()
    {
        var product = Product.Create("SKU-001", "Produto de teste", 10);

        Assert.Throws<InvalidOperationException>(() => product.AdjustStock(-11));
        Assert.Equal(10, product.AvailableQuantity);
    }

    [Fact]
    public void AdjustStock_WhenProductIsInactive_IsRejected()
    {
        var product = Product.Create("SKU-001", "Produto de teste", 10);
        product.Update("Produto de teste", false);

        Assert.Throws<InvalidOperationException>(() => product.AdjustStock(1));
    }

    [Fact]
    public void ReserveAndReleaseStock_KeepBalancesConsistent()
    {
        var product = Product.Create("SKU-001", "Produto de teste", 10);

        product.ReserveStock(4);

        Assert.Equal(4, product.ReservedQuantity);
        Assert.Equal(6, product.FreeQuantity);

        product.ReleaseStock(4);

        Assert.Equal(0, product.ReservedQuantity);
        Assert.Equal(10, product.FreeQuantity);
    }

    [Fact]
    public void ReserveStock_BeyondFreeQuantity_IsRejectedWithoutChangingBalance()
    {
        var product = Product.Create("SKU-001", "Produto de teste", 3);

        Assert.Throws<InvalidOperationException>(() => product.ReserveStock(4));
        Assert.Equal(0, product.ReservedQuantity);
        Assert.Equal(3, product.FreeQuantity);
    }

    [Fact]
    public void Update_WithValidData_ChangesNameAndActivity()
    {
        var product = Product.Create("SKU-001", "Produto de teste", 10);

        product.Update("Produto atualizado", false);

        Assert.Equal("Produto atualizado", product.Name);
        Assert.False(product.IsActive);
        Assert.Equal("SKU-001", product.Sku);
    }
}
