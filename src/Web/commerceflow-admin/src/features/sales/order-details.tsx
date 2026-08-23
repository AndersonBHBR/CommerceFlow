"use client";

import { RefreshIcon } from "@/components/icons";
import type { Order } from "@/lib/contracts";
import {
  canCancelOrder,
  formatCurrency,
  formatOrderDate,
  orderStatusClass,
  orderStatusLabel,
} from "@/features/sales/order-format";

type OrderDetailsProps = {
  order: Order;
  refreshing: boolean;
  onRefresh: () => void;
  onCancel: () => void;
};

export function OrderDetails({
  order,
  refreshing,
  onRefresh,
  onCancel,
}: OrderDetailsProps) {
  return (
    <div className="order-details">
      <section className="order-details-hero">
        <div>
          <span>Pedido</span>
          <strong>{order.number}</strong>
          <code>{order.externalReference}</code>
        </div>
        <span className={orderStatusClass(order.status)}>
          {orderStatusLabel(order.status)}
        </span>
      </section>

      {order.status === "PendingStock" ? (
        <div className="async-order-note">
          <span className="loading-spinner" />
          <div>
            <strong>Reserva em processamento</strong>
            <p>RabbitMQ encaminhará o resultado do Estoque de forma assíncrona.</p>
          </div>
        </div>
      ) : null}

      {order.statusReason ? (
        <div className={`status-reason reason-${order.status.toLowerCase()}`}>
          <span>Motivo da situação</span>
          <p>{order.statusReason}</p>
        </div>
      ) : null}

      <section className="order-facts">
        <div>
          <span>Cliente</span>
          <code>{order.customerId}</code>
        </div>
        <div>
          <span>Criado em</span>
          <strong>{formatOrderDate(order.createdAtUtc)}</strong>
        </div>
        <div>
          <span>Responsável</span>
          <code>{order.createdBy}</code>
        </div>
        {order.cancelledAtUtc ? (
          <div>
            <span>Cancelado em</span>
            <strong>{formatOrderDate(order.cancelledAtUtc)}</strong>
          </div>
        ) : null}
      </section>

      <section className="order-detail-items">
        <header>
          <span className="section-index">Itens do pedido</span>
          <strong>{order.items.length}</strong>
        </header>
        <div>
          {order.items.map((item) => (
            <article key={item.id}>
              <div>
                <code>{item.sku}</code>
                <strong>{item.productName}</strong>
              </div>
              <span>{item.quantity} × {formatCurrency(item.unitPrice)}</span>
              <b>{formatCurrency(item.totalAmount)}</b>
            </article>
          ))}
        </div>
        <footer>
          <span>Total</span>
          <strong>{formatCurrency(order.totalAmount)}</strong>
        </footer>
      </section>

      <footer className="order-detail-actions">
        <button
          type="button"
          className="secondary-button"
          onClick={onRefresh}
          disabled={refreshing}
        >
          <RefreshIcon /> {refreshing ? "Atualizando…" : "Atualizar situação"}
        </button>
        {canCancelOrder(order.status) ? (
          <button type="button" className="danger-button" onClick={onCancel}>
            Cancelar pedido
          </button>
        ) : null}
      </footer>
    </div>
  );
}
