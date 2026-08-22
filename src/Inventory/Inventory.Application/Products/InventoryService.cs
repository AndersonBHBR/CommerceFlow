using Inventory.Application.Abstractions;
using Inventory.Application.Common;
using Inventory.Domain.Products;
using Inventory.Domain.Stock;

namespace Inventory.Application.Products;

public sealed class InventoryService(IInventoryRepository repository, TimeProvider timeProvider)
{
    private const int MaximumAdjustmentAttempts = 3;

    public async Task<InventoryResult<ProductResponse>> CreateProductAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        Product product;
        StockAdjustment? initialAdjustment = null;
        try
        {
            product = Product.Create(command.Sku, command.Name, command.InitialQuantity);
            if (command.InitialQuantity > 0)
            {
                initialAdjustment = StockAdjustment.Create(
                    product.Id,
                    command.InitialQuantity,
                    "Saldo inicial do produto.",
                    command.PerformedBy,
                    null,
                    timeProvider.GetUtcNow());
            }
            else
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(command.PerformedBy);
            }
        }
        catch (ArgumentException exception)
        {
            return Validation<ProductResponse>(exception.Message);
        }

        if (await repository.SkuExistsAsync(product.Sku, cancellationToken))
        {
            return InventoryResult.Failure<ProductResponse>(
                InventoryErrorCode.DuplicateSku,
                "Já existe um produto cadastrado com esse SKU.");
        }

        repository.AddProduct(product);
        if (initialAdjustment is not null)
        {
            repository.AddAdjustment(initialAdjustment);
        }

        var saveOutcome = await repository.SaveChangesAsync(cancellationToken);
        return saveOutcome switch
        {
            SaveOutcome.Success => InventoryResult.Success(ProductResponse.From(product)),
            SaveOutcome.DuplicateSku => InventoryResult.Failure<ProductResponse>(
                InventoryErrorCode.DuplicateSku,
                "Já existe um produto cadastrado com esse SKU."),
            _ => InventoryResult.Failure<ProductResponse>(
                InventoryErrorCode.ConcurrencyConflict,
                "O produto foi alterado durante a operação. Tente novamente.")
        };
    }

    public async Task<InventoryResult<ProductResponse>> UpdateProductAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var versionResult = DecodeRowVersion(command.RowVersion);
        if (!versionResult.IsSuccess)
        {
            return InventoryResult.Failure<ProductResponse>(
                versionResult.Error!.Code,
                versionResult.Error.Message);
        }

        var product = await repository.GetProductByIdAsync(command.ProductId, true, cancellationToken);
        if (product is null)
        {
            return NotFound<ProductResponse>();
        }

        try
        {
            product.Update(command.Name, command.IsActive);
        }
        catch (ArgumentException exception)
        {
            return Validation<ProductResponse>(exception.Message);
        }

        repository.SetExpectedRowVersion(product, versionResult.Value!);
        var saveOutcome = await repository.SaveChangesAsync(cancellationToken);
        return saveOutcome == SaveOutcome.Success
            ? InventoryResult.Success(ProductResponse.From(product))
            : InventoryResult.Failure<ProductResponse>(
                InventoryErrorCode.ConcurrencyConflict,
                "A versão informada está desatualizada. Consulte o produto e tente novamente.");
    }

    public async Task<InventoryResult<ProductResponse>> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetProductByIdAsync(productId, false, cancellationToken);
        return product is null
            ? NotFound<ProductResponse>()
            : InventoryResult.Success(ProductResponse.From(product));
    }

    public async Task<InventoryResult<ProductResponse>> GetProductBySkuAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        string normalizedSku;
        try
        {
            normalizedSku = Product.NormalizeSku(sku);
        }
        catch (ArgumentException exception)
        {
            return Validation<ProductResponse>(exception.Message);
        }

        var product = await repository.GetProductBySkuAsync(normalizedSku, cancellationToken);
        return product is null
            ? NotFound<ProductResponse>()
            : InventoryResult.Success(ProductResponse.From(product));
    }

    public async Task<InventoryResult<PagedResult<ProductResponse>>> ListProductsAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var paginationError = ValidatePagination(page, pageSize);
        if (paginationError is not null)
        {
            return Validation<PagedResult<ProductResponse>>(paginationError);
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (normalizedSearch?.Length > 100)
        {
            return Validation<PagedResult<ProductResponse>>("A busca deve possuir no máximo 100 caracteres.");
        }

        var (items, totalCount) = await repository.ListProductsAsync(
            page,
            pageSize,
            normalizedSearch,
            cancellationToken);

        return InventoryResult.Success(
            new PagedResult<ProductResponse>(
                items.Select(ProductResponse.From).ToArray(),
                page,
                pageSize,
                totalCount));
    }

    public async Task<InventoryResult<StockAdjustmentCreatedResponse>> AdjustStockAsync(
        AdjustStockCommand command,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaximumAdjustmentAttempts; attempt++)
        {
            var product = await repository.GetProductByIdAsync(command.ProductId, true, cancellationToken);
            if (product is null)
            {
                return NotFound<StockAdjustmentCreatedResponse>();
            }

            StockAdjustment adjustment;
            try
            {
                product.AdjustStock(command.Quantity);
                adjustment = StockAdjustment.Create(
                    product.Id,
                    command.Quantity,
                    command.Reason,
                    command.PerformedBy,
                    command.ExternalReference,
                    timeProvider.GetUtcNow());
            }
            catch (ArgumentException exception)
            {
                return Validation<StockAdjustmentCreatedResponse>(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return InventoryResult.Failure<StockAdjustmentCreatedResponse>(
                    InventoryErrorCode.Conflict,
                    exception.Message);
            }

            repository.AddAdjustment(adjustment);
            var saveOutcome = await repository.SaveChangesAsync(cancellationToken);
            if (saveOutcome == SaveOutcome.Success)
            {
                return InventoryResult.Success(
                    new StockAdjustmentCreatedResponse(
                        StockAdjustmentResponse.From(adjustment),
                        ProductResponse.From(product)));
            }

            repository.ClearTracking();
            if (saveOutcome != SaveOutcome.ConcurrencyConflict || attempt == MaximumAdjustmentAttempts)
            {
                break;
            }
        }

        return InventoryResult.Failure<StockAdjustmentCreatedResponse>(
            InventoryErrorCode.ConcurrencyConflict,
            "O estoque recebeu alterações concorrentes. Consulte o produto e tente novamente.");
    }

    public async Task<InventoryResult<PagedResult<StockAdjustmentResponse>>> ListAdjustmentsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var paginationError = ValidatePagination(page, pageSize);
        if (paginationError is not null)
        {
            return Validation<PagedResult<StockAdjustmentResponse>>(paginationError);
        }

        var product = await repository.GetProductByIdAsync(productId, false, cancellationToken);
        if (product is null)
        {
            return NotFound<PagedResult<StockAdjustmentResponse>>();
        }

        var (items, totalCount) = await repository.ListAdjustmentsAsync(
            productId,
            page,
            pageSize,
            cancellationToken);

        return InventoryResult.Success(
            new PagedResult<StockAdjustmentResponse>(
                items.Select(StockAdjustmentResponse.From).ToArray(),
                page,
                pageSize,
                totalCount));
    }

    private static InventoryResult<byte[]> DecodeRowVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Validation<byte[]>("A versão do produto é obrigatória.");
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length == 8
                ? InventoryResult.Success(bytes)
                : Validation<byte[]>("A versão do produto é inválida.");
        }
        catch (FormatException)
        {
            return Validation<byte[]>("A versão do produto é inválida.");
        }
    }

    private static string? ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            return "A página deve ser maior ou igual a 1.";
        }

        return pageSize is < 1 or > 100
            ? "O tamanho da página deve ficar entre 1 e 100."
            : null;
    }

    private static InventoryResult<T> Validation<T>(string message) =>
        InventoryResult.Failure<T>(InventoryErrorCode.Validation, message);

    private static InventoryResult<T> NotFound<T>() =>
        InventoryResult.Failure<T>(InventoryErrorCode.NotFound, "Produto não encontrado.");
}
