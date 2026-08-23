"use client";

import { useCallback, useEffect, useState } from "react";
import type { PagedResult, Product, StockAdjustment } from "@/lib/contracts";
import {
  listStockAdjustments,
  messageFromError,
} from "@/features/inventory/inventory-api";

type AdjustmentHistoryProps = {
  product: Product;
};

const emptyResult: PagedResult<StockAdjustment> = {
  items: [],
  page: 1,
  pageSize: 10,
  totalCount: 0,
  totalPages: 0,
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "America/Sao_Paulo",
  }).format(new Date(value));
}

export function AdjustmentHistory({ product }: AdjustmentHistoryProps) {
  const [result, setResult] = useState(emptyResult);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadHistory = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setResult(await listStockAdjustments(product.id, page));
    } catch (loadError) {
      setError(messageFromError(loadError));
    } finally {
      setLoading(false);
    }
  }, [page, product.id]);

  useEffect(() => {
    let active = true;

    listStockAdjustments(product.id, page)
      .then((history) => {
        if (active) {
          setResult(history);
          setError(null);
        }
      })
      .catch((loadError: unknown) => {
        if (active) {
          setError(messageFromError(loadError));
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [page, product.id]);

  return (
    <div className="history-view">
      <div className="product-context compact-context">
        <span>{product.sku}</span>
        <strong>{product.name}</strong>
        <div>
          <small>Total de movimentos</small>
          <b>{result.totalCount}</b>
        </div>
      </div>

      {loading ? <div className="panel-state">Carregando movimentações…</div> : null}
      {error ? (
        <div className="panel-state error-state">
          <p>{error}</p>
          <button type="button" className="secondary-button" onClick={loadHistory}>
            Tentar novamente
          </button>
        </div>
      ) : null}

      {!loading && !error && result.items.length === 0 ? (
        <div className="panel-state">
          <strong>Nenhuma movimentação registrada</strong>
          <p>Os ajustes manuais e o saldo inicial aparecerão aqui.</p>
        </div>
      ) : null}

      {!loading && !error && result.items.length > 0 ? (
        <div className="history-list">
          {result.items.map((adjustment) => {
            const inbound = adjustment.quantity > 0;
            return (
              <article className="history-item" key={adjustment.id}>
                <span className={`movement-value ${inbound ? "is-inbound" : "is-outbound"}`}>
                  {inbound ? "+" : ""}{adjustment.quantity}
                </span>
                <div>
                  <strong>{adjustment.reason}</strong>
                  <p>
                    {formatDate(adjustment.occurredAtUtc)} · {adjustment.performedBy}
                  </p>
                  {adjustment.externalReference ? (
                    <code>{adjustment.externalReference}</code>
                  ) : null}
                </div>
              </article>
            );
          })}
        </div>
      ) : null}

      {result.totalPages > 1 ? (
        <footer className="panel-pagination">
          <button
            type="button"
            className="secondary-button"
            disabled={page <= 1 || loading}
            onClick={() => {
              setLoading(true);
              setPage((current) => current - 1);
            }}
          >
            Anterior
          </button>
          <span>{result.page} de {result.totalPages}</span>
          <button
            type="button"
            className="secondary-button"
            disabled={page >= result.totalPages || loading}
            onClick={() => {
              setLoading(true);
              setPage((current) => current + 1);
            }}
          >
            Próxima
          </button>
        </footer>
      ) : null}
    </div>
  );
}
