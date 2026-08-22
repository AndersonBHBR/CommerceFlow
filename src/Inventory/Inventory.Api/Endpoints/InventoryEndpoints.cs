using System.Diagnostics;
using System.Security.Claims;
using CommerceFlow.ServiceDefaults;
using Inventory.Application.Common;
using Inventory.Application.Products;

namespace Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var products = endpoints.MapGroup("/api/v1/products")
            .WithTags("Products");

        products.MapPost("", CreateProductAsync)
            .RequireAuthorization(SecurityPolicies.Inventory)
            .WithName("CreateProduct");

        products.MapGet("", ListProductsAsync)
            .RequireAuthorization(SecurityPolicies.Authenticated)
            .WithName("ListProducts");

        products.MapGet("/{productId:guid}", GetProductByIdAsync)
            .RequireAuthorization(SecurityPolicies.Authenticated)
            .WithName("GetProductById");

        products.MapGet("/by-sku/{sku}", GetProductBySkuAsync)
            .RequireAuthorization(SecurityPolicies.Authenticated)
            .WithName("GetProductBySku");

        products.MapPut("/{productId:guid}", UpdateProductAsync)
            .RequireAuthorization(SecurityPolicies.Inventory)
            .WithName("UpdateProduct");

        products.MapPost("/{productId:guid}/stock-adjustments", AdjustStockAsync)
            .RequireAuthorization(SecurityPolicies.Inventory)
            .WithName("AdjustStock");

        products.MapGet("/{productId:guid}/stock-adjustments", ListAdjustmentsAsync)
            .RequireAuthorization(SecurityPolicies.Authenticated)
            .WithName("ListStockAdjustments");

        return endpoints;
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductRequest request,
        ClaimsPrincipal principal,
        InventoryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Sku,
            request.Name,
            request.InitialQuantity,
            GetActor(principal));

        var result = await service.CreateProductAsync(command, cancellationToken);
        return InventoryEndpointResults.From(
            result,
            httpContext,
            product => Results.Created($"/api/v1/products/{product.Id}", product));
    }

    private static async Task<IResult> ListProductsAsync(
        int? page,
        int? pageSize,
        string? search,
        InventoryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.ListProductsAsync(
            page ?? 1,
            pageSize ?? 20,
            search,
            cancellationToken);

        return InventoryEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> GetProductByIdAsync(
        Guid productId,
        InventoryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.GetProductByIdAsync(productId, cancellationToken);
        return InventoryEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> GetProductBySkuAsync(
        string sku,
        InventoryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.GetProductBySkuAsync(sku, cancellationToken);
        return InventoryEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> UpdateProductAsync(
        Guid productId,
        UpdateProductRequest request,
        InventoryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            productId,
            request.Name,
            request.IsActive,
            request.RowVersion);

        var result = await service.UpdateProductAsync(command, cancellationToken);
        return InventoryEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> AdjustStockAsync(
        Guid productId,
        AdjustStockRequest request,
        ClaimsPrincipal principal,
        InventoryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new AdjustStockCommand(
            productId,
            request.Quantity,
            request.Reason,
            request.ExternalReference,
            GetActor(principal));

        var result = await service.AdjustStockAsync(command, cancellationToken);
        return InventoryEndpointResults.From(
            result,
            httpContext,
            response => Results.Created(
                $"/api/v1/products/{productId}/stock-adjustments/{response.Adjustment.Id}",
                response));
    }

    private static async Task<IResult> ListAdjustmentsAsync(
        Guid productId,
        int? page,
        int? pageSize,
        InventoryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAdjustmentsAsync(
            productId,
            page ?? 1,
            pageSize ?? 20,
            cancellationToken);

        return InventoryEndpointResults.From(result, httpContext, Results.Ok);
    }

    private static string GetActor(ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value
        ?? principal.Identity?.Name
        ?? "authenticated-user";
}

public sealed record CreateProductRequest(string Sku, string Name, int InitialQuantity);

public sealed record UpdateProductRequest(string Name, bool IsActive, string RowVersion);

public sealed record AdjustStockRequest(int Quantity, string Reason, string? ExternalReference);

internal static class InventoryEndpointResults
{
    public static IResult From<T>(
        InventoryResult<T> result,
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
            InventoryErrorCode.Validation => (StatusCodes.Status422UnprocessableEntity, "Dados inválidos"),
            InventoryErrorCode.NotFound => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            InventoryErrorCode.DuplicateSku => (StatusCodes.Status409Conflict, "SKU duplicado"),
            InventoryErrorCode.Conflict => (StatusCodes.Status409Conflict, "Operação não permitida"),
            InventoryErrorCode.ConcurrencyConflict => (StatusCodes.Status409Conflict, "Conflito de concorrência"),
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
