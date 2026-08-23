import { gatewayBffRequest } from "@/lib/bff";

type RouteContext = {
  params: Promise<{ orderId: string }>;
};

export async function GET(_request: Request, context: RouteContext) {
  const { orderId } = await context.params;
  return gatewayBffRequest(`/api/v1/orders/${encodeURIComponent(orderId)}`);
}
