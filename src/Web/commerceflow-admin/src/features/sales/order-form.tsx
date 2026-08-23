"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { PlusIcon, TrashIcon } from "@/components/icons";
import type { Product } from "@/lib/contracts";
import { listProducts, messageFromError } from "@/features/inventory/inventory-api";
import {
  createOrder,
  messageFromSalesError,
} from "@/features/sales/sales-api";
import { formatCurrency } from "@/features/sales/order-format";
import type { Order } from "@/lib/contracts";

type DraftItem = {
  key: string;
  productId: string;
  quantity: string;
  unitPrice: string;
};

type OrderFormProps = {
  onCancel: () => void;
  onCreated: (order: Order) => void;
};

function newDraftItem(): DraftItem {
  return {
    key: crypto.randomUUID(),
    productId: "",
    quantity: "1",
    unitPrice: "",
  };
}

export function OrderForm({ onCancel, onCreated }: OrderFormProps) {
  const [customerId, setCustomerId] = useState("");
  const [externalReference, setExternalReference] = useState("");
  const [items, setItems] = useState<DraftItem[]>(() => [newDraftItem()]);
  const [catalog, setCatalog] = useState<Product[]>([]);
  const [catalogLoading, setCatalogLoading] = useState(true);
  const [catalogError, setCatalogError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;

    listProducts({ page: 1, pageSize: 100 })
      .then((result) => {
        if (active) {
          setCatalog(
            result.items.filter((product) => product.isActive && product.freeQuantity > 0),
          );
          setCatalogError(null);
        }
      })
      .catch((loadError: unknown) => {
        if (active) {
          setCatalogError(messageFromError(loadError));
        }
      })
      .finally(() => {
        if (active) {
          setCatalogLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const selectedProductIds = useMemo(
    () => new Set(items.map((item) => item.productId).filter(Boolean)),
    [items],
  );

  const orderTotal = useMemo(
    () =>
      items.reduce(
        (total, item) =>
          total + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0),
        0,
      ),
    [items],
  );

  function generateDemonstrationData() {
    setCustomerId(crypto.randomUUID());
    setExternalReference(`WEB-${Date.now()}`);
  }

  function updateItem(key: string, changes: Partial<DraftItem>) {
    setItems((current) =>
      current.map((item) => (item.key === key ? { ...item, ...changes } : item)),
    );
  }

  function removeItem(key: string) {
    setItems((current) => current.filter((item) => item.key !== key));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (new Set(items.map((item) => item.productId)).size !== items.length) {
      setError("O mesmo produto não pode aparecer mais de uma vez no pedido.");
      return;
    }

    setSubmitting(true);

    try {
      const order = await createOrder({
        customerId,
        externalReference: externalReference.trim(),
        items: items.map((item) => ({
          productId: item.productId,
          quantity: Number(item.quantity),
          unitPrice: Number(item.unitPrice),
        })),
      });
      onCreated(order);
    } catch (submissionError) {
      setError(messageFromSalesError(submissionError));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="entity-form order-form" onSubmit={handleSubmit}>
      <div className="form-callout">
        <div>
          <strong>Dados de demonstração</strong>
          <p>Gere um cliente e uma referência únicos para o teste funcional.</p>
        </div>
        <button type="button" className="secondary-button" onClick={generateDemonstrationData}>
          Gerar identificadores
        </button>
      </div>

      <label className="field-group">
        <span>Identificador do cliente</span>
        <input
          value={customerId}
          onChange={(event) => setCustomerId(event.target.value)}
          pattern="[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"
          placeholder="UUID do cliente"
          required
        />
      </label>

      <label className="field-group">
        <span>Referência externa</span>
        <input
          value={externalReference}
          onChange={(event) => setExternalReference(event.target.value.toUpperCase())}
          minLength={3}
          maxLength={100}
          placeholder="PEDIDO-ERP-2026-001"
          required
        />
        <small>Chave idempotente: repetir o mesmo conteúdo não cria outro pedido.</small>
      </label>

      <section className="order-items-editor">
        <header>
          <div>
            <span className="section-index">Itens</span>
            <strong>{items.length} de 100</strong>
          </div>
          <button
            type="button"
            className="secondary-button"
            disabled={items.length >= Math.min(100, catalog.length)}
            onClick={() => setItems((current) => [...current, newDraftItem()])}
          >
            <PlusIcon /> Adicionar item
          </button>
        </header>

        {catalogLoading ? <div className="inline-state">Carregando catálogo…</div> : null}
        {catalogError ? <p className="form-error">{catalogError}</p> : null}
        {!catalogLoading && !catalogError && catalog.length === 0 ? (
          <p className="form-error">
            Não há produtos ativos com saldo livre. Ajuste o Estoque antes de criar o pedido.
          </p>
        ) : null}

        <div className="order-item-list">
          {items.map((item, index) => {
            const product = catalog.find((candidate) => candidate.id === item.productId);
            const itemTotal = (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0);

            return (
              <article className="order-item-editor" key={item.key}>
                <header>
                  <span>Item {index + 1}</span>
                  <button
                    type="button"
                    aria-label={`Remover item ${index + 1}`}
                    disabled={items.length === 1}
                    onClick={() => removeItem(item.key)}
                  >
                    <TrashIcon />
                  </button>
                </header>

                <label className="field-group item-product-field">
                  <span>Produto</span>
                  <select
                    value={item.productId}
                    onChange={(event) => updateItem(item.key, { productId: event.target.value })}
                    required
                  >
                    <option value="">Selecione um produto</option>
                    {catalog.map((candidate) => (
                      <option
                        key={candidate.id}
                        value={candidate.id}
                        disabled={
                          selectedProductIds.has(candidate.id) && candidate.id !== item.productId
                        }
                      >
                        {candidate.sku} · {candidate.name} · livre {candidate.freeQuantity}
                      </option>
                    ))}
                  </select>
                  {product ? <small>Saldo livre atual: {product.freeQuantity}</small> : null}
                </label>

                <label className="field-group">
                  <span>Quantidade</span>
                  <input
                    type="number"
                    min="1"
                    max={product?.freeQuantity ?? 100000}
                    step="1"
                    value={item.quantity}
                    onChange={(event) => updateItem(item.key, { quantity: event.target.value })}
                    required
                  />
                </label>

                <label className="field-group">
                  <span>Preço unitário</span>
                  <input
                    type="number"
                    min="0.01"
                    max="1000000"
                    step="0.01"
                    value={item.unitPrice}
                    onChange={(event) => updateItem(item.key, { unitPrice: event.target.value })}
                    placeholder="0,00"
                    required
                  />
                </label>

                <div className="order-item-total">
                  <span>Subtotal</span>
                  <strong>{formatCurrency(itemTotal)}</strong>
                </div>
              </article>
            );
          })}
        </div>
      </section>

      <div className="order-total-summary">
        <span>Total do pedido</span>
        <strong>{formatCurrency(orderTotal)}</strong>
      </div>

      {error ? <p className="form-error">{error}</p> : null}

      <footer className="form-actions">
        <button type="button" className="secondary-button" onClick={onCancel}>
          Cancelar
        </button>
        <button
          type="submit"
          className="primary-button compact"
          disabled={submitting || catalogLoading || catalog.length === 0}
        >
          {submitting ? "Criando pedido…" : "Criar pedido"}
        </button>
      </footer>
    </form>
  );
}
