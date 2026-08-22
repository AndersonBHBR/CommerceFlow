import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "CommerceFlow Operations Console",
    template: "%s | CommerceFlow",
  },
  description:
    "Console operacional para gestão de vendas, estoque e saúde da plataforma CommerceFlow.",
  robots: {
    index: false,
    follow: false,
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="pt-BR">
      <body>{children}</body>
    </html>
  );
}
