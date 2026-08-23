export type TokenResponse = {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  expiresAtUtc: string;
};

export type CurrentUser = {
  subject: string;
  name: string;
  roles: string[];
};

export type ServiceHealth = {
  key: "gateway" | "identity" | "sales" | "inventory";
  name: string;
  description: string;
  status: "operational" | "unavailable";
  latencyMs: number | null;
};

export type ApiProblem = {
  title?: string;
  detail?: string;
  status?: number;
  traceId?: string;
};

export type Product = {
  id: string;
  sku: string;
  name: string;
  isActive: boolean;
  availableQuantity: number;
  reservedQuantity: number;
  freeQuantity: number;
  rowVersion: string;
};

export type StockAdjustment = {
  id: string;
  productId: string;
  quantity: number;
  reason: string;
  performedBy: string;
  externalReference: string | null;
  occurredAtUtc: string;
};

export type StockAdjustmentCreated = {
  adjustment: StockAdjustment;
  product: Product;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type OrderStatus =
  | "PendingStock"
  | "Confirmed"
  | "Rejected"
  | "Cancelled";

export type OrderItem = {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
};

export type Order = {
  id: string;
  number: string;
  customerId: string;
  externalReference: string;
  status: OrderStatus;
  statusReason: string | null;
  totalAmount: number;
  createdBy: string;
  createdAtUtc: string;
  cancelledBy: string | null;
  cancelledAtUtc: string | null;
  rowVersion: string;
  items: OrderItem[];
};
