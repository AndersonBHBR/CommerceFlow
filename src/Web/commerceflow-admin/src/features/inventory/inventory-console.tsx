"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import {
  EditIcon,
  HistoryIcon,
  PlusIcon,
  RefreshIcon,
  SearchIcon,
  StockIcon,
} from "@/components/icons";
import type { PagedResult, Product } from "@/lib/contracts";
import {
  listProducts,
  messageFromError,
} from "@/features/inventory/inventory-api";
import { AdjustmentHistory } from "@/features/inventory/adjustment-history";
import { ProductForm } from "@/features/inventory/product-form";
import { SidePanel } from "@/components/side-panel";
import { StockAdjustmentForm } from "@/features/inventory/stock-adjustment-form";

type ActivePanel =
  | { kind: "create" }
  | { kind: "edit"; product: Product }
  | { kind: "adjust"; product: Product }
  | { kind: "history"; product: Product };

const pageSize = 10;
const emptyResult: PagedResult<Product> = {
  items: [],
  page: 1,
  pageSize,
  totalCount: 0,
  totalPages: 0,
};

export function InventoryConsole() {
  const [result, setResult] = useState(emptyResult);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [activePanel, setActivePanel] = useState<ActivePanel | null>(null);

  const loadProducts = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const products = await listProducts({ page, pageSize, search });
      setResult(products);

      if (products.totalPages > 0 && page > products.totalPages) {
        setPage(products.totalPages);
      }
    } catch (loadError) {
      setError(messageFromError(loadError));
    } finally {
      setLoading(false);
    }
  }, [page, search]);

  useEffect(() => {
    let active = true;

    listProducts({ page, pageSize, search })
      .then((products) => {
        if (!active) {
          return;
        }

        setResult(products);
        setError(null);
        if (products.totalPages > 0 && page > products.totalPages) {
          setPage(products.totalPages);
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
  }, [page, search]);

  useEffect(() => {
    if (!feedback) {
      return;
    }

    const timeoutId = window.setTimeout(() => setFeedback(null), 5000);
    return () => window.clearTimeout(timeoutId);
  }, [feedback]);

  const visibleTotals = useMemo(
    () =>
      result.items.reduce(
        (totals, product) => ({
          free: totals.free + product.freeQuantity,
          reserved: totals.reserved + product.reservedQuantity,
          active: totals.active + (product.isActive ? 1 : 0),
        }),
        { free: 0, reserved: 0, active: 0 },
      ),
    [result.items],
  );

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextSearch = searchInput.trim();

    if (page === 1 && nextSearch === search) {
      void loadProducts();
      return;
    }

    setLoading(true);
    setError(null);
    setPage(1);
    setSearch(nextSearch);
  }

  function handleSaved(product: Product, message: string) {
    setResult((current) => ({
      ...current,
      items: current.items.map((item) => (item.id === product.id ? product : item)),
    }));
    setActivePanel(null);
    setFeedback(message);
    void loadProducts();
  }

  return (
    <main className="inventory-page">
      <header className="inventory-heading">
        <div>
          <span className="eyebrow">Domínio de estoque</span>
          <h1>Produtos e saldos</h1>
          <p>Cadastre produtos, acompanhe reservas e registre movimentações auditáveis.</p>
        </div>
        <button
          type="button"
          className="primary-button inventory-create-button"
          onClick={() => setActivePanel({ kind: "create" })}
        >
          <PlusIcon />
          Novo produto
        </button>
      </header>

      <section className="inventory-metrics" aria-label="Resumo da página atual">
        <article>
          <span>Produtos cadastrados</span>
          <strong>{result.totalCount}</strong>
          <small>Total encontrado</small>
        </article>
        <article>
          <span>Ativos nesta página</span>
          <strong>{visibleTotals.active}</strong>
          <small>de {result.items.length} registros visíveis</small>
        </article>
        <article>
          <span>Saldo livre visível</span>
          <strong>{visibleTotals.free}</strong>
          <small>Disponível para novos pedidos</small>
        </article>
        <article>
          <span>Reservado visível</span>
          <strong>{visibleTotals.reserved}</strong>
          <small>Comprometido por vendas</small>
        </article>
      </section>

      {feedback ? (
        <div className="feedback-banner" role="status">
          <span />
          {feedback}
          <button type="button" onClick={() => setFeedback(null)} aria-label="Fechar aviso">
            ×
          </button>
        </div>
      ) : null}

      <section className="inventory-surface">
        <header className="inventory-toolbar">
          <form className="inventory-search" onSubmit={handleSearch}>
            <SearchIcon />
            <input
              type="search"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              maxLength={100}
              placeholder="Buscar por SKU ou nome"
              aria-label="Buscar produtos por SKU ou nome"
            />
            <button type="submit">Buscar</button>
          </form>
          <button
            type="button"
            className="icon-button refresh-button"
            onClick={() => void loadProducts()}
            aria-label="Atualizar produtos"
            title="Atualizar produtos"
            disabled={loading}
          >
            <RefreshIcon />
          </button>
        </header>

        {error ? (
          <div className="inventory-state error-state">
            <strong>Não foi possível carregar o estoque</strong>
            <p>{error}</p>
            <button type="button" className="secondary-button" onClick={loadProducts}>
              Tentar novamente
            </button>
          </div>
        ) : null}

        {!error && loading && result.items.length === 0 ? (
          <div className="inventory-state loading-state">
            <span className="loading-spinner" />
            Consultando o serviço de Estoque…
          </div>
        ) : null}

        {!error && !loading && result.items.length === 0 ? (
          <div className="inventory-state empty-state">
            <span className="empty-icon"><StockIcon /></span>
            <strong>{search ? "Nenhum produto corresponde à busca" : "Estoque ainda vazio"}</strong>
            <p>
              {search
                ? "Revise o termo informado ou limpe a pesquisa."
                : "Cadastre o primeiro produto para iniciar a operação."}
            </p>
            {!search ? (
              <button
                type="button"
                className="secondary-button"
                onClick={() => setActivePanel({ kind: "create" })}
              >
                Cadastrar primeiro produto
              </button>
            ) : null}
          </div>
        ) : null}

        {!error && result.items.length > 0 ? (
          <div className={`inventory-table-wrap ${loading ? "is-refreshing" : ""}`}>
            <table className="inventory-table">
              <thead>
                <tr>
                  <th>Produto</th>
                  <th>Situação</th>
                  <th className="numeric-column">Disponível</th>
                  <th className="numeric-column">Reservado</th>
                  <th className="numeric-column">Livre</th>
                  <th><span className="visually-hidden">Ações</span></th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((product) => (
                  <tr key={product.id}>
                    <td>
                      <div className="product-cell">
                        <code>{product.sku}</code>
                        <strong>{product.name}</strong>
                      </div>
                    </td>
                    <td>
                      <span className={`product-status ${product.isActive ? "is-active" : "is-inactive"}`}>
                        {product.isActive ? "Ativo" : "Inativo"}
                      </span>
                    </td>
                    <td className="numeric-column">{product.availableQuantity}</td>
                    <td className="numeric-column reserved-value">{product.reservedQuantity}</td>
                    <td className="numeric-column free-value">{product.freeQuantity}</td>
                    <td>
                      <div className="row-actions">
                        <button
                          type="button"
                          onClick={() => setActivePanel({ kind: "adjust", product })}
                          aria-label={`Ajustar estoque de ${product.name}`}
                          title="Ajustar estoque"
                        >
                          <StockIcon />
                        </button>
                        <button
                          type="button"
                          onClick={() => setActivePanel({ kind: "history", product })}
                          aria-label={`Consultar histórico de ${product.name}`}
                          title="Histórico"
                        >
                          <HistoryIcon />
                        </button>
                        <button
                          type="button"
                          onClick={() => setActivePanel({ kind: "edit", product })}
                          aria-label={`Editar ${product.name}`}
                          title="Editar produto"
                        >
                          <EditIcon />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}

        {!error && result.totalCount > 0 ? (
          <footer className="inventory-pagination">
            <span>
              Página <strong>{result.page}</strong> de <strong>{Math.max(1, result.totalPages)}</strong>
              <small>{result.totalCount} produto(s)</small>
            </span>
            <div>
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
            </div>
          </footer>
        ) : null}
      </section>

      {activePanel?.kind === "create" ? (
        <SidePanel eyebrow="Catálogo" title="Cadastrar produto" onClose={() => setActivePanel(null)}>
          <ProductForm onCancel={() => setActivePanel(null)} onSaved={handleSaved} />
        </SidePanel>
      ) : null}

      {activePanel?.kind === "edit" ? (
        <SidePanel eyebrow="Catálogo" title="Editar produto" onClose={() => setActivePanel(null)}>
          <ProductForm
            product={activePanel.product}
            onCancel={() => setActivePanel(null)}
            onSaved={handleSaved}
          />
        </SidePanel>
      ) : null}

      {activePanel?.kind === "adjust" ? (
        <SidePanel eyebrow="Movimentação" title="Ajustar estoque" onClose={() => setActivePanel(null)}>
          <StockAdjustmentForm
            product={activePanel.product}
            onCancel={() => setActivePanel(null)}
            onSaved={handleSaved}
          />
        </SidePanel>
      ) : null}

      {activePanel?.kind === "history" ? (
        <SidePanel
          eyebrow="Auditoria"
          title="Histórico de movimentações"
          onClose={() => setActivePanel(null)}
          wide
        >
          <AdjustmentHistory product={activePanel.product} />
        </SidePanel>
      ) : null}
    </main>
  );
}
