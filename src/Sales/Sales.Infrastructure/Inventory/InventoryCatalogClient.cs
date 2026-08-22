using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sales.Application.Abstractions;

namespace Sales.Infrastructure.Inventory;

public sealed class InventoryCatalogClient(HttpClient httpClient) : IInventoryCatalog
{
    public async Task<InventoryLookupResult> GetProductAsync(
        Guid productId,
        string authorization,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/products/{productId}");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new InventoryLookupResult(InventoryLookupStatus.NotFound, null, null);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new InventoryLookupResult(
                    InventoryLookupStatus.Unavailable,
                    null,
                    $"O serviço de Estoque respondeu com HTTP {(int)response.StatusCode}.");
            }

            var document = await response.Content.ReadFromJsonAsync<InventoryProductDocument>(
                cancellationToken: cancellationToken);
            if (document is null || document.Id != productId)
            {
                return new InventoryLookupResult(
                    InventoryLookupStatus.Unavailable,
                    null,
                    "O serviço de Estoque devolveu uma resposta inválida.");
            }

            return new InventoryLookupResult(
                InventoryLookupStatus.Found,
                new InventoryProductSnapshot(
                    document.Id,
                    document.Sku,
                    document.Name,
                    document.IsActive,
                    document.FreeQuantity),
                null);
        }
        catch (HttpRequestException exception)
        {
            return Unavailable(exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable(exception.Message);
        }
        catch (JsonException exception)
        {
            return Unavailable(exception.Message);
        }
        catch (NotSupportedException exception)
        {
            return Unavailable(exception.Message);
        }
    }

    private static InventoryLookupResult Unavailable(string detail) =>
        new(InventoryLookupStatus.Unavailable, null, $"Falha ao consultar o Estoque: {detail}");

    private sealed record InventoryProductDocument(
        Guid Id,
        string Sku,
        string Name,
        bool IsActive,
        int FreeQuantity);
}
