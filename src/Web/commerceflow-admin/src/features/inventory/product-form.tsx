"use client";

import { useState, type FormEvent } from "react";
import type { Product } from "@/lib/contracts";
import {
  createProduct,
  messageFromError,
  updateProduct,
} from "@/features/inventory/inventory-api";

type ProductFormProps = {
  product?: Product;
  onCancel: () => void;
  onSaved: (product: Product, message: string) => void;
};

export function ProductForm({ product, onCancel, onSaved }: ProductFormProps) {
  const editing = Boolean(product);
  const [sku, setSku] = useState(product?.sku ?? "");
  const [name, setName] = useState(product?.name ?? "");
  const [initialQuantity, setInitialQuantity] = useState("0");
  const [isActive, setIsActive] = useState(product?.isActive ?? true);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      if (product) {
        const updated = await updateProduct(product.id, {
          name: name.trim(),
          isActive,
          rowVersion: product.rowVersion,
        });
        onSaved(updated, "Produto atualizado com controle de concorrência.");
      } else {
        const created = await createProduct({
          sku: sku.trim(),
          name: name.trim(),
          initialQuantity: Number(initialQuantity),
        });
        onSaved(created, "Produto cadastrado e disponível no estoque.");
      }
    } catch (submissionError) {
      setError(messageFromError(submissionError));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      {editing ? (
        <div className="immutable-field">
          <span>SKU imutável</span>
          <strong>{product?.sku}</strong>
        </div>
      ) : (
        <label className="field-group">
          <span>SKU</span>
          <input
            value={sku}
            onChange={(event) => setSku(event.target.value.toUpperCase())}
            minLength={3}
            maxLength={64}
            pattern="[A-Za-z0-9._-]+"
            placeholder="EX.: NOTEBOOK-PRO-15"
            autoFocus
            required
          />
          <small>3–64 caracteres: letras, números, ponto, hífen ou sublinhado.</small>
        </label>
      )}

      <label className="field-group">
        <span>Nome do produto</span>
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          minLength={2}
          maxLength={200}
          placeholder="Nome comercial do produto"
          autoFocus={editing}
          required
        />
      </label>

      {editing ? (
        <label className="switch-field">
          <span>
            <strong>Produto ativo</strong>
            <small>Produtos inativos não aceitam novos ajustes de estoque.</small>
          </span>
          <input
            type="checkbox"
            checked={isActive}
            onChange={(event) => setIsActive(event.target.checked)}
          />
        </label>
      ) : (
        <label className="field-group">
          <span>Saldo inicial</span>
          <input
            type="number"
            min="0"
            step="1"
            value={initialQuantity}
            onChange={(event) => setInitialQuantity(event.target.value)}
            required
          />
          <small>Informe zero quando o primeiro recebimento ocorrer posteriormente.</small>
        </label>
      )}

      {error ? <p className="form-error">{error}</p> : null}

      <footer className="form-actions">
        <button type="button" className="secondary-button" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="primary-button compact" disabled={submitting}>
          {submitting ? "Salvando…" : editing ? "Salvar alterações" : "Cadastrar produto"}
        </button>
      </footer>
    </form>
  );
}
