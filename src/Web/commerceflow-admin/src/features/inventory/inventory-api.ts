import type {
  ApiProblem,
  PagedResult,
  Product,
  StockAdjustment,
  StockAdjustmentCreated,
} from "@/lib/contracts";

export class InventoryApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly traceId?: string,
  ) {
    super(message);
    this.name = "InventoryApiError";
  }
}

async function requestJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
    cache: "no-store",
  });

  if (response.status === 401) {
    window.location.replace("/login");
    throw new InventoryApiError("Sua sessão expirou.", 401);
  }

  if (!response.ok) {
    let problem: ApiProblem = {};

    try {
      problem = (await response.json()) as ApiProblem;
    } catch {
      // Mantém a mensagem de contingência quando a resposta não é JSON.
    }

    throw new InventoryApiError(
      problem.detail ?? problem.title ?? "Não foi possível concluir a operação.",
      response.status,
      problem.traceId,
    );
  }

  return (await response.json()) as T;
}

export function listProducts(params: {
  page: number;
  pageSize: number;
  search?: string;
}): Promise<PagedResult<Product>> {
  const query = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  });

  if (params.search) {
    query.set("search", params.search);
  }

  return requestJson(`/api/inventory/products?${query.toString()}`);
}

export function createProduct(input: {
  sku: string;
  name: string;
  initialQuantity: number;
}): Promise<Product> {
  return requestJson("/api/inventory/products", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateProduct(
  productId: string,
  input: { name: string; isActive: boolean; rowVersion: string },
): Promise<Product> {
  return requestJson(`/api/inventory/products/${productId}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function adjustProductStock(
  productId: string,
  input: { quantity: number; reason: string; externalReference: string | null },
): Promise<StockAdjustmentCreated> {
  return requestJson(`/api/inventory/products/${productId}/adjustments`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function listStockAdjustments(
  productId: string,
  page: number,
  pageSize = 10,
): Promise<PagedResult<StockAdjustment>> {
  const query = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });

  return requestJson(
    `/api/inventory/products/${productId}/adjustments?${query.toString()}`,
  );
}

export function messageFromError(error: unknown): string {
  return error instanceof Error
    ? error.message
    : "Ocorreu uma falha inesperada. Tente novamente.";
}
