import {
  forbiddenOriginResponse,
  gatewayBffRequest,
  isSameOrigin,
} from "@/lib/bff";

type RouteContext = {
  params: Promise<{ orderId: string }>;
};

export async function POST(request: Request, context: RouteContext) {
  if (!isSameOrigin(request)) {
    return forbiddenOriginResponse();
  }

  const { orderId } = await context.params;
  return gatewayBffRequest(`/api/v1/orders/${encodeURIComponent(orderId)}/cancel`, {
    method: "POST",
    body: await request.text(),
  });
}
