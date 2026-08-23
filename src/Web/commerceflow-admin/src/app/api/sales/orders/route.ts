import {
  forbiddenOriginResponse,
  gatewayBffRequest,
  isSameOrigin,
  orderListQuery,
} from "@/lib/bff";

export async function GET(request: Request) {
  return gatewayBffRequest(`/api/v1/orders${orderListQuery(request)}`);
}

export async function POST(request: Request) {
  if (!isSameOrigin(request)) {
    return forbiddenOriginResponse();
  }

  return gatewayBffRequest("/api/v1/orders", {
    method: "POST",
    body: await request.text(),
  });
}
