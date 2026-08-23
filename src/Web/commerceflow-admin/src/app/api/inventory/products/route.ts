import {
  forbiddenOriginResponse,
  inventoryGatewayRequest,
  inventoryListQuery,
  isSameOrigin,
} from "@/lib/bff";

export async function GET(request: Request) {
  return inventoryGatewayRequest(
    `/api/v1/products${inventoryListQuery(request)}`,
  );
}

export async function POST(request: Request) {
  if (!isSameOrigin(request)) {
    return forbiddenOriginResponse();
  }

  return inventoryGatewayRequest("/api/v1/products", {
    method: "POST",
    body: await request.text(),
  });
}
