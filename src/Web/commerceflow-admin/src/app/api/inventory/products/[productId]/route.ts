import {
  forbiddenOriginResponse,
  inventoryGatewayRequest,
  isSameOrigin,
} from "@/lib/bff";

type RouteContext = {
  params: Promise<{ productId: string }>;
};

export async function GET(_request: Request, context: RouteContext) {
  const { productId } = await context.params;
  return inventoryGatewayRequest(`/api/v1/products/${encodeURIComponent(productId)}`);
}

export async function PUT(request: Request, context: RouteContext) {
  if (!isSameOrigin(request)) {
    return forbiddenOriginResponse();
  }

  const { productId } = await context.params;
  return inventoryGatewayRequest(`/api/v1/products/${encodeURIComponent(productId)}`, {
    method: "PUT",
    body: await request.text(),
  });
}
