"use client";

import { useMemo, useState, type FormEvent } from "react";
import type { Product } from "@/lib/contracts";
import {
  adjustProductStock,
  messageFromError,
} from "@/features/inventory/inventory-api";

type StockAdjustmentFormProps = {
  product: Product;
  onCancel: () => void;
  onSaved: (product: Product, message: string) => void;
};

export function StockAdjustmentForm({
  product,
  onCancel,
  onSaved,
}: StockAdjustmentFormProps) {
  const [quantity, setQuantity] = useState("1");
  const [reason, setReason] = useState("");
  const [externalReference, setExternalReference] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const numericQuantity = Number(quantity);

  const projectedQuantity = useMemo(
    () => product.availableQuantity + (Number.isFinite(numericQuantity) ? numericQuantity : 0),
    [numericQuantity, product.availableQuantity],
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (numericQuantity === 0) {
      setError("A quantidade deve ser diferente de zero.");
      return;
    }

    setSubmitting(true);

    try {
      const result = await adjustProductStock(product.id, {
        quantity: numericQuantity,
        reason: reason.trim(),
        externalReference: externalReference.trim() || null,
      });
      const direction = numericQuantity > 0 ? "Entrada" : "Saída";
      onSaved(
        result.product,
        `${direction} de ${Math.abs(numericQuantity)} unidade(s) registrada.`,
      );
    } catch (submissionError) {
      setError(messageFromError(submissionError));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <div className="product-context">
        <span>{product.sku}</span>
        <strong>{product.name}</strong>
        <div>
          <small>Disponível agora</small>
          <b>{product.availableQuantity}</b>
        </div>
      </div>

      <label className="field-group">
        <span>Quantidade do ajuste</span>
        <input
          type="number"
          step="1"
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          autoFocus
          required
        />
        <small>Use valor positivo para entrada e negativo para saída.</small>
      </label>

      <div className={`stock-projection ${projectedQuantity < product.reservedQuantity ? "is-invalid" : ""}`}>
        <span>Saldo projetado</span>
        <strong>{projectedQuantity}</strong>
        <small>{product.reservedQuantity} unidade(s) reservada(s)</small>
      </div>

      <label className="field-group">
        <span>Motivo</span>
        <textarea
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          minLength={3}
          maxLength={500}
          rows={4}
          placeholder="Ex.: recebimento do fornecedor ou correção de inventário"
          required
        />
      </label>

      <label className="field-group">
        <span>Referência externa <em>opcional</em></span>
        <input
          value={externalReference}
          onChange={(event) => setExternalReference(event.target.value)}
          maxLength={100}
          placeholder="NF-2026-001 ou chamado interno"
        />
      </label>

      {!product.isActive ? (
        <p className="form-error">Reative o produto antes de ajustar seu estoque.</p>
      ) : null}
      {error ? <p className="form-error">{error}</p> : null}

      <footer className="form-actions">
        <button type="button" className="secondary-button" onClick={onCancel}>
          Cancelar
        </button>
        <button
          type="submit"
          className="primary-button compact"
          disabled={submitting || !product.isActive}
        >
          {submitting ? "Registrando…" : "Registrar ajuste"}
        </button>
      </footer>
    </form>
  );
}
