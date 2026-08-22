import http from 'k6/http';
import { check, sleep } from 'k6';

const gatewayUrl = __ENV.GATEWAY_URL || 'http://gateway:8080';
const identityUrl = __ENV.IDENTITY_URL || 'http://identity:8080';
const salesUrl = __ENV.SALES_URL || 'http://sales:8080';
const inventoryUrl = __ENV.INVENTORY_URL || 'http://inventory:8080';

export const options = {
  scenarios: {
    authenticated_reads: {
      executor: 'constant-vus',
      vus: 10,
      duration: '30s',
    },
  },
  thresholds: {
    checks: ['rate>0.99'],
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<750'],
  },
};

function token(login) {
  const response = http.post(
    `${identityUrl}/api/v1/auth/token`,
    JSON.stringify({ login, password: 'CommerceFlow#2026' }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'token' } },
  );
  check(response, { 'token emitido': (result) => result.status === 200 });
  return response.json('accessToken');
}

export function setup() {
  const gatewayHealth = http.get(`${gatewayUrl}/health/ready`, { tags: { endpoint: 'gateway-ready' } });
  check(gatewayHealth, { 'gateway pronto': (result) => result.status === 200 });

  const inventoryToken = token('inventory@commerceflow.local');
  const salesToken = token('sales@commerceflow.local');
  const suffix = `${Date.now()}-${Math.floor(Math.random() * 100000)}`;
  const product = http.post(
    `${inventoryUrl}/api/v1/products`,
    JSON.stringify({
      sku: `LOAD-${suffix}`,
      name: 'Produto do teste de carga',
      initialQuantity: 1000,
    }),
    {
      headers: {
        Authorization: `Bearer ${inventoryToken}`,
        'Content-Type': 'application/json',
      },
      tags: { endpoint: 'create-product' },
    },
  );
  check(product, { 'produto de carga criado': (result) => result.status === 201 });

  return {
    inventoryToken,
    salesToken,
    productId: product.json('id'),
  };
}

export default function (data) {
  const productResponse = http.get(
    `${inventoryUrl}/api/v1/products/${data.productId}`,
    {
      headers: { Authorization: `Bearer ${data.inventoryToken}` },
      tags: { endpoint: 'get-product' },
    },
  );
  check(productResponse, { 'produto consultado': (result) => result.status === 200 });

  const ordersResponse = http.get(
    `${salesUrl}/api/v1/orders?page=1&pageSize=20`,
    {
      headers: { Authorization: `Bearer ${data.salesToken}` },
      tags: { endpoint: 'list-orders' },
    },
  );
  check(ordersResponse, { 'pedidos consultados': (result) => result.status === 200 });
  sleep(0.25);
}
