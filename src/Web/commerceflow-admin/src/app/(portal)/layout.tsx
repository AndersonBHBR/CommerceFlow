import Link from "next/link";
import { redirect } from "next/navigation";
import { Brand } from "@/components/brand";
import {
  ActivityIcon,
  BoxIcon,
  DashboardIcon,
  OrdersIcon,
} from "@/components/icons";
import { LogoutButton } from "@/components/logout-button";
import { getCurrentUser } from "@/lib/auth";

export default async function PortalLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const user = await getCurrentUser();
  if (!user) {
    redirect("/login");
  }

  const displayName = user.name?.split("@")[0] || "operador";

  return (
    <div className="portal-shell">
      <aside className="sidebar">
        <Brand />

        <nav className="primary-nav" aria-label="Navegação principal">
          <span className="nav-caption">Operação</span>
          <Link href="/dashboard">
            <DashboardIcon />
            <span>Visão geral</span>
          </Link>
          <span className="nav-item-disabled" aria-disabled="true">
            <BoxIcon />
            <span>Estoque</span>
            <small>em breve</small>
          </span>
          <span className="nav-item-disabled" aria-disabled="true">
            <OrdersIcon />
            <span>Pedidos</span>
            <small>em breve</small>
          </span>

          <span className="nav-caption nav-caption-spaced">Plataforma</span>
          <a href="http://localhost:18888" target="_blank" rel="noreferrer">
            <ActivityIcon />
            <span>Observabilidade</span>
          </a>
        </nav>

        <div className="sidebar-footer">
          <div className="user-summary">
            <span className="avatar">{displayName.slice(0, 2).toUpperCase()}</span>
            <span>
              <strong>{displayName}</strong>
              <small>{user.roles.join(" · ")}</small>
            </span>
          </div>
          <LogoutButton />
        </div>
      </aside>

      <div className="portal-content">
        <header className="mobile-header">
          <Brand compact />
          <div className="mobile-user">
            <span>{displayName}</span>
            <LogoutButton />
          </div>
        </header>
        {children}
      </div>
    </div>
  );
}
