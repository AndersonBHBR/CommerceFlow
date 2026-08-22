using System.Diagnostics;
using System.Security.Claims;
using CommerceFlow.ServiceDefaults;
using Sales.Application.Common;
using Sales.Application.Orders;
using Sales.Domain.Orders;

namespace Sales.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var orders = endpoints.MapGroup("/api/v1/orders")
            .RequireAuthorization(SecurityPolicies.Sales)
            .WithTags("Orders");

        orders.MapPost("", CreateOrderAsync)
            .WithName("CreateOrder");

        orders.MapGet("", ListOrdersAsync)
            .WithName("ListOrders");

        orders.MapGet("/{orderId:guid}", GetOrderByIdAsync)
            .WithName("GetOrderById");

        orders.MapPost("/{orderId:guid}/cancel", CancelOrderAsync)
            .WithName("CancelOrder");

        return endpoints;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        ClaimsPrincipal principal,
        OrderService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.CustomerId,
            request.ExternalReference,
            request.Items?.Select(item => new CreateOrderItemCommand(
                item.ProductId,
                item.Quantity,
                item.UnitPrice)).ToArray() ?? [],
            GetActor(principal),
            httpContext.Request.Headers.Authorization.ToString(),
            httpContext.TraceIdentifier);

        var result = await service.CreateAsync(command, cancellationToken);
        return SalesEndpointResults.From(
            result,
            httpContext,
            response => response.IsReplay
                ? Results.Ok(response.Order)
                : Results.Created($"/api/v1/orders/{response.Order.Id}", response.Order));
    }

    private static async Task<IResult> ListOrdersAsync(
        int? page,
        int? pageSize,
        Guid? customerId,
        OrderStatus? status,
        OrderService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(
            page ?? 1,
            pageSize ?? 20,
            customerId,
            status,
            cancellationToken);

        return SalesEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> GetOrderByIdAsync(
        Guid orderId,
        OrderService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(orderId, cancellationToken);
        return SalesEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> CancelOrderAsync(
        Guid orderId,
        CancelOrderRequest request,
        ClaimsPrincipal principal,
        OrderService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CancelOrderCommand(
            orderId,
            request.RowVersion,
            GetActor(principal),
            httpContext.TraceIdentifier);

        var result = await service.CancelAsync(command, cancellationToken);
        return SalesEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static string GetActor(ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value
        ?? principal.Identity?.Name
        ?? "authenticated-user";
}

public sealed record CreateOrderRequest(
    Guid CustomerId,
    string ExternalReference,
    IReadOnlyCollection<CreateOrderItemRequest>? Items);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public sealed record CancelOrderRequest(string RowVersion);

internal static class SalesEndpointResults
{
    public static IResult From<T>(
        SalesResult<T> result,
        HttpContext httpContext,
        Func<T, IResult> success)
    {
        if (result.IsSuccess)
        {
            return success(result.Value!);
        }

        var error = result.Error!;
        var (status, title) = error.Code switch
        {
            SalesErrorCode.Validation => (StatusCodes.Status422UnprocessableEntity, "Dados inválidos"),
            SalesErrorCode.NotFound => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            SalesErrorCode.Conflict => (StatusCodes.Status409Conflict, "Operação não permitida"),
            SalesErrorCode.DuplicateExternalReference => (StatusCodes.Status409Conflict, "Referência duplicada"),
            SalesErrorCode.ConcurrencyConflict => (StatusCodes.Status409Conflict, "Conflito de concorrência"),
            SalesErrorCode.InventoryUnavailable => (StatusCodes.Status503ServiceUnavailable, "Estoque indisponível"),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado")
        };

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return Results.Problem(
            statusCode: status,
            title: title,
            detail: error.Message,
            instance: httpContext.Request.Path,
            extensions: new Dictionary<string, object?> { ["traceId"] = traceId });
    }
}
