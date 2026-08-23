"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import {
  CancelIcon,
  EyeIcon,
  FilterIcon,
  OrdersIcon,
  PlusIcon,
  RefreshIcon,
} from "@/components/icons";
import { SidePanel } from "@/components/side-panel";
import type { Order, OrderStatus, PagedResult } from "@/lib/contracts";
import { CancelOrderForm } from "@/features/sales/cancel-order-form";
import { OrderDetails } from "@/features/sales/order-details";
import { OrderForm } from "@/features/sales/order-form";
import {
  formatCurrency,
  formatOrderDate,
  orderStatusClass,
  orderStatusLabel,
  orderStatusOptions,
  canCancelOrder,
} from "@/features/sales/order-format";
import {
  getOrder,
  listOrders,
  messageFromSalesError,
} from "@/features/sales/sales-api";

type ActivePanel =
  | { kind: "create" }
  | { kind: "details"; order: Order }
  | { kind: "cancel"; order: Order };

const pageSize = 10;
const emptyResult: PagedResult<Order> = {
  items: [],
  page: 1,
  pageSize,
  totalCount: 0,
  totalPages: 0,
};

export function OrdersConsole() {
  const [result, setResult] = useState(emptyResult);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<OrderStatus | "">("");
  const [statusDraft, setStatusDraft] = useState<OrderStatus | "">("");
  const [customerId, setCustomerId] = useState("");
  const [customerDraft, setCustomerDraft] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [activePanel, setActivePanel] = useState<ActivePanel | null>(null);
  const [detailRefreshing, setDetailRefreshing] = useState(false);

  const loadOrders = useCallback(async (showLoading = true) => {
    if (showLoading) {
      setLoading(true);
    }
    setError(null);

    try {
      const orders = await listOrders({ page, pageSize, customerId, status });
      setResult(orders);
      if (orders.totalPages > 0 && page > orders.totalPages) {
        setPage(orders.totalPages);
      }
    } catch (loadError) {
      setError(messageFromSalesError(loadError));
    } finally {
      if (showLoading) {
        setLoading(false);
      }
    }
  }, [customerId, page, status]);

  useEffect(() => {
    let active = true;

    listOrders({ page, pageSize, customerId, status })
      .then((orders) => {
        if (!active) {
          return;
        }

        setResult(orders);
        setError(null);
        if (orders.totalPages > 0 && page > orders.totalPages) {
          setPage(orders.totalPages);
        }
      })
      .catch((loadError: unknown) => {
        if (active) {
          setError(messageFromSalesError(loadError));
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
  }, [customerId, page, status]);

  const hasPendingOrders = result.items.some((order) => order.status === "PendingStock");

  useEffect(() => {
    if (!hasPendingOrders) {
      return;
    }

    const intervalId = window.setInterval(() => {
      listOrders({ page, pageSize, customerId, status })
        .then(setResult)
        .catch(() => undefined);
    }, 4000);

    return () => window.clearInterval(intervalId);
  }, [customerId, hasPendingOrders, page, status]);

  useEffect(() => {
    if (!feedback) {
      return;
    }

    const timeoutId = window.setTimeout(() => setFeedback(null), 6000);
    return () => window.clearTimeout(timeoutId);
  }, [feedback]);

  const visibleMetrics = useMemo(
    () =>
      result.items.reduce(
        (metrics, order) => ({
          pending: metrics.pending + (order.status === "PendingStock" ? 1 : 0),
          confirmed: metrics.confirmed + (order.status === "Confirmed" ? 1 : 0),
          value: metrics.value + order.totalAmount,
        }),
        { pending: 0, confirmed: 0, value: 0 },
      ),
    [result.items],
  );

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextCustomer = customerDraft.trim();

    if (page === 1 && nextCustomer === customerId && statusDraft === status) {
      void loadOrders();
      return;
    }

    setLoading(true);
    setError(null);
    setPage(1);
    setCustomerId(nextCustomer);
    setStatus(statusDraft);
  }

  function clearFilters() {
    setCustomerDraft("");
    setStatusDraft("");
    setCustomerId("");
    setStatus("");
    setPage(1);
    setLoading(true);
  }

  function updateVisibleOrder(order: Order) {
    setResult((current) => ({
      ...current,
      items: current.items.map((item) => (item.id === order.id ? order : item)),
    }));
  }

  function handleCreated(order: Order) {
    setFeedback(
      `Pedido ${order.number} criado. Aguardando confirmação assíncrona do Estoque.`,
    );
    setActivePanel({ kind: "details", order });
    setPage(1);
    void loadOrders(false);
  }

  async function refreshDetails() {
    if (!activePanel || activePanel.kind !== "details") {
      return;
    }

    setDetailRefreshing(true);
    try {
      const refreshed = await getOrder(activePanel.order.id);
      setActivePanel({ kind: "details", order: refreshed });
      updateVisibleOrder(refreshed);
    } catch (refreshError) {
      setFeedback(messageFromSalesError(refreshError));
    } finally {
      setDetailRefreshing(false);
    }
  }

  function handleCancelled(order: Order) {
    updateVisibleOrder(order);
    setActivePanel({ kind: "details", order });
    setFeedback(`Pedido ${order.number} cancelado com sucesso.`);
    void loadOrders(false);
  }

  return (
    <main className="orders-page">
      <header className="orders-heading">
        <div>
          <span className="eyebrow">Domínio de vendas</span>
          <h1>Pedidos e operação</h1>
          <p>Crie pedidos, acompanhe a reserva assíncrona e execute cancelamentos seguros.</p>
        </div>
        <button
          type="button"
          className="primary-button orders-create-button"
          onClick={() => setActivePanel({ kind: "create" })}
        >
          <PlusIcon /> Novo pedido
        </button>
      </header>

      <section className="orders-metrics" aria-label="Resumo dos pedidos visíveis">
        <article>
          <span>Pedidos encontrados</span>
          <strong>{result.totalCount}</strong>
          <small>Total com os filtros atuais</small>
        </article>
        <article>
          <span>Aguardando estoque</span>
          <strong>{visibleMetrics.pending}</strong>
          <small>Atualização automática ativa</small>
        </article>
        <article>
          <span>Confirmados visíveis</span>
          <strong>{visibleMetrics.confirmed}</strong>
          <small>Reserva concluída</small>
        </article>
        <article>
          <span>Valor da página</span>
          <strong className="currency-metric">{formatCurrency(visibleMetrics.value)}</strong>
          <small>Soma dos registros visíveis</small>
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

      <section className="orders-surface">
        <header className="orders-toolbar">
          <form className="orders-filter" onSubmit={applyFilters}>
            <FilterIcon />
            <select
              value={statusDraft}
              onChange={(event) => setStatusDraft(event.target.value as OrderStatus | "")}
              aria-label="Filtrar por situação"
            >
              <option value="">Todas as situações</option>
              {orderStatusOptions.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
            <input
              value={customerDraft}
              onChange={(event) => setCustomerDraft(event.target.value)}
              placeholder="UUID do cliente (opcional)"
              aria-label="Filtrar por identificador do cliente"
            />
            <button type="submit">Aplicar filtros</button>
          </form>
          <div className="toolbar-actions">
            {(status || customerId) ? (
              <button type="button" className="secondary-button" onClick={clearFilters}>
                Limpar
              </button>
            ) : null}
            <button
              type="button"
              className="icon-button"
              onClick={() => void loadOrders()}
              aria-label="Atualizar pedidos"
              title="Atualizar pedidos"
              disabled={loading}
            >
              <RefreshIcon />
            </button>
          </div>
        </header>

        {hasPendingOrders ? (
          <div className="auto-refresh-note">
            <span className="loading-spinner" />
            Atualizando pedidos pendentes a cada quatro segundos
          </div>
        ) : null}

        {error ? (
          <div className="inventory-state error-state">
            <strong>Não foi possível carregar os pedidos</strong>
            <p>{error}</p>
            <button type="button" className="secondary-button" onClick={() => void loadOrders()}>
              Tentar novamente
            </button>
          </div>
        ) : null}

        {!error && loading && result.items.length === 0 ? (
          <div className="inventory-state loading-state">
            <span className="loading-spinner" />
            Consultando o serviço de Vendas…
          </div>
        ) : null}

        {!error && !loading && result.items.length === 0 ? (
          <div className="inventory-state empty-state">
            <span className="empty-icon empty-icon-orange"><OrdersIcon /></span>
            <strong>Nenhum pedido encontrado</strong>
            <p>
              {status || customerId
                ? "Revise os filtros ou consulte todos os pedidos."
                : "Crie o primeiro pedido para iniciar a operação de Vendas."}
            </p>
            {!status && !customerId ? (
              <button
                type="button"
                className="secondary-button"
                onClick={() => setActivePanel({ kind: "create" })}
              >
                Criar primeiro pedido
              </button>
            ) : null}
          </div>
        ) : null}

        {!error && result.items.length > 0 ? (
          <div className={`orders-table-wrap ${loading ? "is-refreshing" : ""}`}>
            <table className="orders-table">
              <thead>
                <tr>
                  <th>Pedido</th>
                  <th>Situação</th>
                  <th>Cliente</th>
                  <th className="numeric-column">Itens</th>
                  <th className="numeric-column">Total</th>
                  <th>Criado em</th>
                  <th><span className="visually-hidden">Ações</span></th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((order) => (
                  <tr key={order.id}>
                    <td>
                      <div className="order-cell">
                        <strong>{order.number}</strong>
                        <code>{order.externalReference}</code>
                      </div>
                    </td>
                    <td>
                      <span className={orderStatusClass(order.status)}>
                        {orderStatusLabel(order.status)}
                      </span>
                    </td>
                    <td><code className="customer-code">{order.customerId}</code></td>
                    <td className="numeric-column">{order.items.length}</td>
                    <td className="numeric-column order-value">{formatCurrency(order.totalAmount)}</td>
                    <td>{formatOrderDate(order.createdAtUtc)}</td>
                    <td>
                      <div className="row-actions">
                        <button
                          type="button"
                          onClick={() => setActivePanel({ kind: "details", order })}
                          aria-label={`Consultar ${order.number}`}
                          title="Consultar pedido"
                        >
                          <EyeIcon />
                        </button>
                        {canCancelOrder(order.status) ? (
                          <button
                            type="button"
                            className="cancel-row-action"
                            onClick={() => setActivePanel({ kind: "cancel", order })}
                            aria-label={`Cancelar ${order.number}`}
                            title="Cancelar pedido"
                          >
                            <CancelIcon />
                          </button>
                        ) : null}
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
              <small>{result.totalCount} pedido(s)</small>
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
        <SidePanel eyebrow="Nova venda" title="Criar pedido" onClose={() => setActivePanel(null)} wide>
          <OrderForm onCancel={() => setActivePanel(null)} onCreated={handleCreated} />
        </SidePanel>
      ) : null}

      {activePanel?.kind === "details" ? (
        <SidePanel eyebrow="Operação" title="Detalhes do pedido" onClose={() => setActivePanel(null)} wide>
          <OrderDetails
            order={activePanel.order}
            refreshing={detailRefreshing}
            onRefresh={() => void refreshDetails()}
            onCancel={() => setActivePanel({ kind: "cancel", order: activePanel.order })}
          />
        </SidePanel>
      ) : null}

      {activePanel?.kind === "cancel" ? (
        <SidePanel eyebrow="Ação crítica" title="Cancelar pedido" onClose={() => setActivePanel(null)}>
          <CancelOrderForm
            order={activePanel.order}
            onBack={() => setActivePanel({ kind: "details", order: activePanel.order })}
            onCancelled={handleCancelled}
          />
        </SidePanel>
      ) : null}
    </main>
  );
}
