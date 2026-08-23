"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  ActivityIcon,
  BoxIcon,
  DashboardIcon,
  OrdersIcon,
} from "@/components/icons";

export function PortalNav({ mobile = false }: { mobile?: boolean }) {
  const pathname = usePathname();

  if (mobile) {
    return (
      <nav className="mobile-nav" aria-label="Navegação móvel">
        <Link href="/dashboard" className={pathname === "/dashboard" ? "active" : ""}>
          <DashboardIcon />
          Visão geral
        </Link>
        <Link href="/estoque" className={pathname.startsWith("/estoque") ? "active" : ""}>
          <BoxIcon />
          Estoque
        </Link>
        <Link href="/pedidos" className={pathname.startsWith("/pedidos") ? "active" : ""}>
          <OrdersIcon />
          Pedidos
        </Link>
        <Link
          href="/observabilidade"
          className={pathname.startsWith("/observabilidade") ? "active" : ""}
        >
          <ActivityIcon />
          Saúde
        </Link>
      </nav>
    );
  }

  return (
    <nav className="primary-nav" aria-label="Navegação principal">
      <span className="nav-caption">Operação</span>
      <Link href="/dashboard" className={pathname === "/dashboard" ? "active" : ""}>
        <DashboardIcon />
        <span>Visão geral</span>
      </Link>
      <Link href="/estoque" className={pathname.startsWith("/estoque") ? "active" : ""}>
        <BoxIcon />
        <span>Estoque</span>
      </Link>
      <Link href="/pedidos" className={pathname.startsWith("/pedidos") ? "active" : ""}>
        <OrdersIcon />
        <span>Pedidos</span>
      </Link>

      <span className="nav-caption nav-caption-spaced">Plataforma</span>
      <Link
        href="/observabilidade"
        className={pathname.startsWith("/observabilidade") ? "active" : ""}
      >
        <ActivityIcon />
        <span>Observabilidade</span>
      </Link>
    </nav>
  );
}
