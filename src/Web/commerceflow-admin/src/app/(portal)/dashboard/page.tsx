import { ArrowIcon, BoxIcon, OrdersIcon } from "@/components/icons";
import { ServiceStatusCard } from "@/components/service-status-card";
import { getPlatformHealth } from "@/lib/commerceflow-api";

function formatTimestamp(date: Date) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "America/Sao_Paulo",
  }).format(date);
}

export default async function DashboardPage() {
  const services = await getPlatformHealth();
  const operationalCount = services.filter(
    (service) => service.status === "operational",
  ).length;
  const platformOperational = operationalCount === services.length;

  return (
    <main className="dashboard-page">
      <header className="page-heading">
        <div>
          <span className="eyebrow">Centro de operações</span>
          <h1>Visão geral</h1>
          <p>Saúde e acessos rápidos do ecossistema CommerceFlow.</p>
        </div>
        <div className={`global-status ${platformOperational ? "is-up" : "is-down"}`}>
          <span />
          <div>
            <strong>
              {platformOperational ? "Plataforma operacional" : "Atenção necessária"}
            </strong>
            <small>
              {operationalCount}/{services.length} serviços disponíveis
            </small>
          </div>
        </div>
      </header>

      <section className="metric-grid" aria-label="Indicadores principais">
        <article className="metric-card metric-card-highlight">
          <span className="metric-label">Disponibilidade</span>
          <strong>
            {services.length === 0
              ? "0%"
              : `${Math.round((operationalCount / services.length) * 100)}%`}
          </strong>
          <p>Leitura atual dos health checks</p>
        </article>
        <article className="metric-card">
          <span className="metric-label">Serviços monitorados</span>
          <strong>{services.length}</strong>
          <p>Gateway e microsserviços de domínio</p>
        </article>
        <article className="metric-card">
          <span className="metric-label">Atualizado em</span>
          <strong className="metric-time">{formatTimestamp(new Date())}</strong>
          <p>Dados sem cache, coletados nesta requisição</p>
        </article>
      </section>

      <section className="dashboard-section">
        <div className="section-heading">
          <div>
            <span className="section-index">01</span>
            <h2>Saúde dos serviços</h2>
          </div>
          <span className="live-indicator">
            <i /> ao vivo
          </span>
        </div>

        <div className="service-grid">
          {services.map((service) => (
            <ServiceStatusCard key={service.key} service={service} />
          ))}
        </div>
      </section>

      <section className="dashboard-section">
        <div className="section-heading">
          <div>
            <span className="section-index">02</span>
            <h2>Módulos operacionais</h2>
          </div>
          <span className="section-note">próximas entregas do incremento</span>
        </div>

        <div className="module-grid">
          <article className="module-card">
            <span className="module-icon">
              <BoxIcon />
            </span>
            <div>
              <span className="module-state">Preparado para integração</span>
              <h3>Gestão de estoque</h3>
              <p>
                Produtos, saldos livres, reservas e histórico de movimentações.
              </p>
            </div>
            <span className="module-action">
              Próximo módulo <ArrowIcon />
            </span>
          </article>

          <article className="module-card">
            <span className="module-icon module-icon-orange">
              <OrdersIcon />
            </span>
            <div>
              <span className="module-state">Preparado para integração</span>
              <h3>Operação de vendas</h3>
              <p>
                Criação, consulta, situação e cancelamento seguro de pedidos.
              </p>
            </div>
            <span className="module-action">
              Próximo módulo <ArrowIcon />
            </span>
          </article>
        </div>
      </section>
    </main>
  );
}
