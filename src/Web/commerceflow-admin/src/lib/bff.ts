import "server-only";

import { NextResponse } from "next/server";
import { getSessionToken, sessionCookieName } from "@/lib/auth";
import { gatewayFetch } from "@/lib/commerceflow-api";

export function isSameOrigin(request: Request): boolean {
  const origin = request.headers.get("origin");
  if (!origin) {
    return true;
  }

  try {
    return new URL(origin).host === new URL(request.url).host;
  } catch {
    return false;
  }
}

export function forbiddenOriginResponse() {
  return NextResponse.json(
    { title: "Origem não permitida", detail: "A origem da solicitação não é válida." },
    { status: 403 },
  );
}

function copySearchParameters(
  source: URLSearchParams,
  allowedNames: readonly string[],
): string {
  const target = new URLSearchParams();

  for (const name of allowedNames) {
    const value = source.get(name);
    if (value !== null) {
      target.set(name, value);
    }
  }

  const query = target.toString();
  return query ? `?${query}` : "";
}

export function inventoryListQuery(request: Request): string {
  return copySearchParameters(new URL(request.url).searchParams, [
    "page",
    "pageSize",
    "search",
  ]);
}

export function adjustmentListQuery(request: Request): string {
  return copySearchParameters(new URL(request.url).searchParams, ["page", "pageSize"]);
}

async function forwardGatewayResponse(response: Response): Promise<NextResponse> {
  const body = await response.text();
  const outgoing = new NextResponse(body || null, {
    status: response.status,
    headers: {
      "Content-Type": response.headers.get("content-type") ?? "application/json",
      "Cache-Control": "no-store",
    },
  });

  if (response.status === 401) {
    outgoing.cookies.set({
      name: sessionCookieName,
      value: "",
      httpOnly: true,
      sameSite: "lax",
      path: "/",
      maxAge: 0,
    });
  }

  return outgoing;
}

export async function inventoryGatewayRequest(
  path: string,
  init: RequestInit = {},
): Promise<NextResponse> {
  const token = await getSessionToken();
  if (!token) {
    return NextResponse.json(
      { title: "Sessão expirada", detail: "Entre novamente para continuar." },
      { status: 401 },
    );
  }

  try {
    const headers = new Headers(init.headers);
    if (init.body) {
      headers.set("Content-Type", "application/json");
    }

    const response = await gatewayFetch(path, token, {
      ...init,
      headers,
      signal: AbortSignal.timeout(7000),
    });

    return forwardGatewayResponse(response);
  } catch {
    return NextResponse.json(
      {
        title: "Serviço indisponível",
        detail: "O Gateway não respondeu. Confirme se o CommerceFlow está ativo.",
      },
      { status: 503 },
    );
  }
}
