import { redirect } from "next/navigation";
import { Brand } from "@/components/brand";
import { getCurrentUser } from "@/lib/auth";
import { LoginForm } from "@/app/login/login-form";

export default async function LoginPage() {
  const user = await getCurrentUser();
  if (user) {
    redirect("/dashboard");
  }

  return (
    <main className="login-page">
      <section className="login-intro">
        <Brand />
        <div className="intro-content">
          <span className="eyebrow">Operação distribuída, visão unificada</span>
          <h1>Controle o fluxo comercial de ponta a ponta.</h1>
          <p>
            Estoque, vendas e saúde operacional reunidos em um console criado
            para decisões rápidas e rastreáveis.
          </p>
        </div>

        <div className="architecture-strip" aria-label="Fluxo da plataforma">
          <span>Gateway</span>
          <i />
          <span>Vendas</span>
          <i />
          <span>Mensageria</span>
          <i />
          <span>Estoque</span>
        </div>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <div className="login-card-heading">
            <span className="section-index">Acesso seguro</span>
            <h2>Bem-vindo ao CommerceFlow</h2>
            <p>Entre com um usuário habilitado para acessar o console.</p>
          </div>

          <LoginForm />

          <aside className="demo-credentials">
            <span>Credencial administrativa de demonstração</span>
            <strong>admin@commerceflow.local</strong>
            <code>CommerceFlow#2026</code>
          </aside>
        </div>

        <p className="environment-caption">
          Ambiente local · .NET 10 · Next.js 16 · OpenTelemetry
        </p>
      </section>
    </main>
  );
}
