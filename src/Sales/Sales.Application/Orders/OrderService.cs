using CommerceFlow.Contracts.Events;
using CommerceFlow.Contracts.Messaging;
using Sales.Application.Abstractions;
using Sales.Application.Common;
using Sales.Domain.Orders;

namespace Sales.Application.Orders;

public sealed class OrderService(
    IOrderRepository repository,
    IInventoryCatalog inventoryCatalog,
    IIntegrationOutbox integrationOutbox,
    TimeProvider timeProvider)
{
    public async Task<SalesResult<CreateOrderResponse>> CreateAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateCommand(command);
        if (validationError is not null)
        {
            return Validation<CreateOrderResponse>(validationError);
        }

        string externalReference;
        try
        {
            externalReference = Order.NormalizeExternalReference(command.ExternalReference);
        }
        catch (ArgumentException exception)
        {
            return Validation<CreateOrderResponse>(exception.Message);
        }

        var existingOrder = await repository.GetByExternalReferenceAsync(externalReference, cancellationToken);
        if (existingOrder is not null)
        {
            return Matches(existingOrder, command)
                ? SalesResult.Success(new CreateOrderResponse(OrderResponse.From(existingOrder), true))
                : SalesResult.Failure<CreateOrderResponse>(
                    SalesErrorCode.DuplicateExternalReference,
                    "A referência externa já pertence a outro conteúdo de pedido.");
        }

        var requestedItems = command.Items.ToArray();
        var lookupTasks = requestedItems
            .Select(item => inventoryCatalog.GetProductAsync(
                item.ProductId,
                command.Authorization,
                cancellationToken))
            .ToArray();
        var lookupResults = await Task.WhenAll(lookupTasks);

        var draftItems = new List<OrderDraftItem>(requestedItems.Length);
        for (var index = 0; index < requestedItems.Length; index++)
        {
            var requestedItem = requestedItems[index];
            var lookup = lookupResults[index];
            if (lookup.Status == InventoryLookupStatus.Unavailable)
            {
                return SalesResult.Failure<CreateOrderResponse>(
                    SalesErrorCode.InventoryUnavailable,
                    lookup.Detail ?? "O serviço de Estoque está indisponível.");
            }

            if (lookup.Status == InventoryLookupStatus.NotFound || lookup.Product is null)
            {
                return SalesResult.Failure<CreateOrderResponse>(
                    SalesErrorCode.Conflict,
                    $"O produto {requestedItem.ProductId} não foi encontrado no Estoque.");
            }

            var product = lookup.Product;
            if (!product.IsActive)
            {
                return SalesResult.Failure<CreateOrderResponse>(
                    SalesErrorCode.Conflict,
                    $"O produto {product.Sku} está inativo.");
            }

            if (product.FreeQuantity < requestedItem.Quantity)
            {
                return SalesResult.Failure<CreateOrderResponse>(
                    SalesErrorCode.Conflict,
                    $"Saldo livre insuficiente para o produto {product.Sku}. Disponível: {product.FreeQuantity}.");
            }

            draftItems.Add(new OrderDraftItem(
                product.Id,
                product.Sku,
                product.Name,
                requestedItem.Quantity,
                requestedItem.UnitPrice));
        }

        Order order;
        var occurredAtUtc = timeProvider.GetUtcNow();
        try
        {
            order = Order.Create(
                Guid.CreateVersion7(),
                command.CustomerId,
                externalReference,
                command.CreatedBy,
                occurredAtUtc,
                draftItems);
        }
        catch (ArgumentException exception)
        {
            return Validation<CreateOrderResponse>(exception.Message);
        }

        repository.Add(order);
        integrationOutbox.Enqueue(
            new OrderCreatedV1(
                Guid.CreateVersion7(),
                occurredAtUtc,
                command.CorrelationId,
                order.Id,
                order.CustomerId,
                order.Items
                    .Select(item => new OrderItemV1(item.ProductId, item.Quantity))
                    .ToArray()),
            MessagingTopology.OrderCreatedRoutingKey);
        var saveOutcome = await repository.SaveChangesAsync(cancellationToken);
        if (saveOutcome == OrderSaveOutcome.Success)
        {
            return SalesResult.Success(new CreateOrderResponse(OrderResponse.From(order), false));
        }

        if (saveOutcome == OrderSaveOutcome.DuplicateExternalReference)
        {
            existingOrder = await repository.GetByExternalReferenceAsync(externalReference, cancellationToken);
            if (existingOrder is not null && Matches(existingOrder, command))
            {
                return SalesResult.Success(new CreateOrderResponse(OrderResponse.From(existingOrder), true));
            }

            return SalesResult.Failure<CreateOrderResponse>(
                SalesErrorCode.DuplicateExternalReference,
                "A referência externa já foi utilizada.");
        }

        return SalesResult.Failure<CreateOrderResponse>(
            SalesErrorCode.ConcurrencyConflict,
            "O pedido recebeu uma alteração concorrente. Tente novamente.");
    }

    public async Task<SalesResult<OrderResponse>> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(orderId, false, cancellationToken);
        return order is null
            ? NotFound<OrderResponse>()
            : SalesResult.Success(OrderResponse.From(order));
    }

    public async Task<SalesResult<PagedResult<OrderResponse>>> ListAsync(
        int page,
        int pageSize,
        Guid? customerId,
        OrderStatus? status,
        CancellationToken cancellationToken)
    {
        var paginationError = ValidatePagination(page, pageSize);
        if (paginationError is not null)
        {
            return Validation<PagedResult<OrderResponse>>(paginationError);
        }

        if (customerId == Guid.Empty)
        {
            return Validation<PagedResult<OrderResponse>>("O cliente informado é inválido.");
        }

        if (status is not null && !Enum.IsDefined(status.Value))
        {
            return Validation<PagedResult<OrderResponse>>("A situação do pedido é inválida.");
        }

        var (items, totalCount) = await repository.ListAsync(
            page,
            pageSize,
            customerId,
            status,
            cancellationToken);

        return SalesResult.Success(new PagedResult<OrderResponse>(
            items.Select(OrderResponse.From).ToArray(),
            page,
            pageSize,
            totalCount));
    }

    public async Task<SalesResult<OrderResponse>> CancelAsync(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        var versionResult = DecodeRowVersion(command.RowVersion);
        if (!versionResult.IsSuccess)
        {
            return SalesResult.Failure<OrderResponse>(
                versionResult.Error!.Code,
                versionResult.Error.Message);
        }

        var order = await repository.GetByIdAsync(command.OrderId, true, cancellationToken);
        if (order is null)
        {
            return NotFound<OrderResponse>();
        }

        var occurredAtUtc = timeProvider.GetUtcNow();
        try
        {
            order.Cancel(command.CancelledBy, occurredAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Validation<OrderResponse>(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return SalesResult.Failure<OrderResponse>(SalesErrorCode.Conflict, exception.Message);
        }

        repository.SetExpectedRowVersion(order, versionResult.Value!);
        integrationOutbox.Enqueue(
            new StockReleaseRequestedV1(
                Guid.CreateVersion7(),
                occurredAtUtc,
                command.CorrelationId,
                order.Id),
            MessagingTopology.StockReleaseRequestedRoutingKey);
        var saveOutcome = await repository.SaveChangesAsync(cancellationToken);
        return saveOutcome == OrderSaveOutcome.Success
            ? SalesResult.Success(OrderResponse.From(order))
            : SalesResult.Failure<OrderResponse>(
                SalesErrorCode.ConcurrencyConflict,
                "A versão informada está desatualizada. Consulte o pedido e tente novamente.");
    }

    private static string? ValidateCreateCommand(CreateOrderCommand command)
    {
        if (command.CustomerId == Guid.Empty)
        {
            return "O cliente é obrigatório.";
        }

        if (string.IsNullOrWhiteSpace(command.Authorization))
        {
            return "A credencial usada para validar o estoque não foi recebida.";
        }

        if (command.Items is null || command.Items.Count is < 1 or > 100)
        {
            return "O pedido deve possuir entre 1 e 100 itens.";
        }

        if (command.Items.Select(item => item.ProductId).Distinct().Count() != command.Items.Count)
        {
            return "O mesmo produto não pode aparecer mais de uma vez no pedido.";
        }

        foreach (var item in command.Items)
        {
            if (item.ProductId == Guid.Empty)
            {
                return "Todos os itens devem informar um produto.";
            }

            if (item.Quantity is < 1 or > 100_000)
            {
                return "A quantidade de cada item deve ficar entre 1 e 100.000.";
            }

            if (item.UnitPrice is < 0.01m or > 1_000_000m || decimal.Round(item.UnitPrice, 2) != item.UnitPrice)
            {
                return "O preço unitário deve ficar entre 0,01 e 1.000.000,00 e possuir no máximo duas casas decimais.";
            }
        }

        return null;
    }

    private static bool Matches(Order order, CreateOrderCommand command)
    {
        if (order.CustomerId != command.CustomerId || order.Items.Count != command.Items.Count)
        {
            return false;
        }

        var requestedByProduct = command.Items.ToDictionary(item => item.ProductId);
        return order.Items.All(item =>
            requestedByProduct.TryGetValue(item.ProductId, out var requested)
            && requested.Quantity == item.Quantity
            && requested.UnitPrice == item.UnitPrice);
    }

    private static SalesResult<byte[]> DecodeRowVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Validation<byte[]>("A versão do pedido é obrigatória.");
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length == 8
                ? SalesResult.Success(bytes)
                : Validation<byte[]>("A versão do pedido é inválida.");
        }
        catch (FormatException)
        {
            return Validation<byte[]>("A versão do pedido é inválida.");
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

    private static SalesResult<T> Validation<T>(string message) =>
        SalesResult.Failure<T>(SalesErrorCode.Validation, message);

    private static SalesResult<T> NotFound<T>() =>
        SalesResult.Failure<T>(SalesErrorCode.NotFound, "Pedido não encontrado.");
}
