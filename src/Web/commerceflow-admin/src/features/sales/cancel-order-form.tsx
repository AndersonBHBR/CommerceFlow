"use client";

import { useState } from "react";
import type { Order } from "@/lib/contracts";
import {
  cancelOrder,
  messageFromSalesError,
} from "@/features/sales/sales-api";
import { formatCurrency, orderStatusLabel } from "@/features/sales/order-format";

type CancelOrderFormProps = {
  order: Order;
  onBack: () => void;
  onCancelled: (order: Order) => void;
};

export function CancelOrderForm({
  order,
  onBack,
  onCancelled,
}: CancelOrderFormProps) {
  const [confirmed, setConfirmed] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleCancel() {
    setSubmitting(true);
    setError(null);

    try {
      onCancelled(await cancelOrder(order.id, order.rowVersion));
    } catch (cancellationError) {
      setError(messageFromSalesError(cancellationError));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="cancel-order-view">
      <div className="cancellation-warning">
        <span>Operação compensatória</span>
        <h3>Cancelar {order.number}?</h3>
        <p>
          Situação atual: <strong>{orderStatusLabel(order.status)}</strong>. Se o estoque
          já estiver reservado, uma solicitação de liberação será publicada no RabbitMQ.
        </p>
      </div>

      <dl className="cancel-order-summary">
        <div>
          <dt>Referência</dt>
          <dd>{order.externalReference}</dd>
        </div>
        <div>
          <dt>Itens</dt>
          <dd>{order.items.length}</dd>
        </div>
        <div>
          <dt>Total</dt>
          <dd>{formatCurrency(order.totalAmount)}</dd>
        </div>
      </dl>

      <label className="confirmation-check">
        <input
          type="checkbox"
          checked={confirmed}
          onChange={(event) => setConfirmed(event.target.checked)}
        />
        <span>Confirmo que desejo cancelar este pedido.</span>
      </label>

      {error ? <p className="form-error">{error}</p> : null}

      <footer className="form-actions">
        <button type="button" className="secondary-button" onClick={onBack}>
          Voltar
        </button>
        <button
          type="button"
          className="danger-button"
          disabled={!confirmed || submitting}
          onClick={handleCancel}
        >
          {submitting ? "Cancelando…" : "Confirmar cancelamento"}
        </button>
      </footer>
    </div>
  );
}
