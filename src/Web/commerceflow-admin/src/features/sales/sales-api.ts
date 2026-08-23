import type {
  ApiProblem,
  Order,
  OrderStatus,
  PagedResult,
} from "@/lib/contracts";

export type CreateOrderInput = {
  customerId: string;
  externalReference: string;
  items: Array<{
    productId: string;
    quantity: number;
    unitPrice: number;
  }>;
};

export class SalesApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly traceId?: string,
  ) {
    super(message);
    this.name = "SalesApiError";
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
    throw new SalesApiError("Sua sessão expirou.", 401);
  }

  if (!response.ok) {
    let problem: ApiProblem = {};

    try {
      problem = (await response.json()) as ApiProblem;
    } catch {
      // Mantém a mensagem de contingência quando a resposta não é JSON.
    }

    throw new SalesApiError(
      problem.detail ?? problem.title ?? "Não foi possível concluir a operação.",
      response.status,
      problem.traceId,
    );
  }

  return (await response.json()) as T;
}

export function listOrders(params: {
  page: number;
  pageSize: number;
  customerId?: string;
  status?: OrderStatus | "";
}): Promise<PagedResult<Order>> {
  const query = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  });

  if (params.customerId) {
    query.set("customerId", params.customerId);
  }

  if (params.status) {
    query.set("status", params.status);
  }

  return requestJson(`/api/sales/orders?${query.toString()}`);
}

export function getOrder(orderId: string): Promise<Order> {
  return requestJson(`/api/sales/orders/${orderId}`);
}

export function createOrder(input: CreateOrderInput): Promise<Order> {
  return requestJson("/api/sales/orders", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function cancelOrder(orderId: string, rowVersion: string): Promise<Order> {
  return requestJson(`/api/sales/orders/${orderId}/cancel`, {
    method: "POST",
    body: JSON.stringify({ rowVersion }),
  });
}

export function messageFromSalesError(error: unknown): string {
  return error instanceof Error
    ? error.message
    : "Ocorreu uma falha inesperada. Tente novamente.";
}
