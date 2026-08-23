import {
  adjustmentListQuery,
  forbiddenOriginResponse,
  inventoryGatewayRequest,
  isSameOrigin,
} from "@/lib/bff";

type RouteContext = {
  params: Promise<{ productId: string }>;
};

export async function GET(request: Request, context: RouteContext) {
  const { productId } = await context.params;
  return inventoryGatewayRequest(
    `/api/v1/products/${encodeURIComponent(productId)}/stock-adjustments${adjustmentListQuery(request)}`,
  );
}

export async function POST(request: Request, context: RouteContext) {
  if (!isSameOrigin(request)) {
    return forbiddenOriginResponse();
  }

  const { productId } = await context.params;
  return inventoryGatewayRequest(
    `/api/v1/products/${encodeURIComponent(productId)}/stock-adjustments`,
    {
      method: "POST",
      body: await request.text(),
    },
  );
}
