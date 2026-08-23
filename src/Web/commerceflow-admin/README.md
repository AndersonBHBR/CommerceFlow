# CommerceFlow Operations Console

Portal administrativo do CommerceFlow desenvolvido com Next.js 16, App Router e TypeScript.

## Incremento 6.1

- autenticação pelo Gateway;
- padrão Backend for Frontend (BFF);
- JWT protegido em cookie `HttpOnly`;
- dashboard com health checks sem cache;
- layout responsivo e estados de indisponibilidade;
- configuração `standalone` preparada para Docker.

## Incremento 6.2

- módulo de Estoque integrado ao Gateway;
- listagem, pesquisa e paginação de produtos;
- cadastro e edição com validação de domínio;
- atualização protegida por `rowVersion`;
- entradas e saídas de estoque com saldo projetado;
- histórico auditável de movimentações;
- BFF para manter o JWT fora do JavaScript do navegador;
- estados de carregamento, ausência de dados, erro e sucesso;
- navegação responsiva para desktop e dispositivos móveis.

## Incremento 6.3

- módulo de Vendas integrado ao Gateway;
- criação de pedidos com múltiplos produtos;
- catálogo consultado no serviço de Estoque;
- cálculo de subtotais e total em reais;
- referência externa idempotente;
- listagem paginada com filtros por cliente e situação;
- acompanhamento automático de pedidos aguardando estoque;
- estados `PendingStock`, `Confirmed`, `Rejected` e `Cancelled`;
- detalhes completos do pedido e de seus itens;
- cancelamento com `rowVersion` e liberação compensatória via RabbitMQ;
- BFF compartilhado para manter o JWT protegido.

## Executar

Com o backend CommerceFlow ativo:

```powershell
Copy-Item .env.example .env.local
npm run dev
```

Acesse `http://localhost:3000` e use:

- login: `admin@commerceflow.local`;
- senha: `CommerceFlow#2026`.

## Segurança

O navegador envia as credenciais somente para uma Route Handler do Next.js. O BFF solicita o token ao Gateway e o mantém em cookie `HttpOnly`, `SameSite=Lax` e `Secure` em produção. O token não é armazenado em `localStorage` nem devolvido ao JavaScript do cliente.

Em desenvolvimento local por HTTP, `COMMERCEFLOW_COOKIE_SECURE=false`. Em uma implantação HTTPS, configure esse valor como `true`.

## 🙋 Sobre o Autor

Feito com 💻 e ☕ por [Anderson Lima Araújo](https://www.linkedin.com/in/anderson-araujo-pcd)😊  
Sou desenvolvedor Full Stack com foco em IA, APIs modernas, soluções web escaláveis e interesse em projetos internacionais 🌍
<p>
    <img align=left margin=10 width=80 src="https://avatars.githubusercontent.com/u/7528140?v=4"/>
    <p>&nbsp&nbsp&nbspAnderson Lima Araújo<br>
    &nbsp&nbsp&nbsp<a href="http://instagram.com/andersonbhbr">Instagram</a>&nbsp;|&nbsp;<a href="https://github.com/AndersonBHBR">GitHub</a>&nbsp;|&nbsp;<a href="https://www.linkedin.com/in/anderson-araujo-pcd/">LinkedIn</a>&nbsp;|&nbsp;<a href="https://www.behance.net/andersonbhbr">Behance</a></p>
</p>
<br/><br/>
