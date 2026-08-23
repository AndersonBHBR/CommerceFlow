import type { OrderStatus } from "@/lib/contracts";

export const orderStatusOptions: Array<{
  value: OrderStatus;
  label: string;
}> = [
  { value: "PendingStock", label: "Aguardando estoque" },
  { value: "Confirmed", label: "Confirmado" },
  { value: "Rejected", label: "Rejeitado" },
  { value: "Cancelled", label: "Cancelado" },
];

export function orderStatusLabel(status: OrderStatus): string {
  return orderStatusOptions.find((option) => option.value === status)?.label ?? status;
}

export function orderStatusClass(status: OrderStatus): string {
  return `order-status status-${status.toLowerCase()}`;
}

export function canCancelOrder(status: OrderStatus): boolean {
  return status === "PendingStock" || status === "Confirmed";
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}

export function formatOrderDate(value: string): string {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "America/Sao_Paulo",
  }).format(new Date(value));
}
