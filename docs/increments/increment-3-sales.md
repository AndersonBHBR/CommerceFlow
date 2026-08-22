# Incremento 3 — Vendas

Este incremento implementa os requisitos `RF-020` a `RF-028`. O fluxo assíncrono preparado aqui foi ativado no Incremento 4.

## Rastreabilidade

| Requisito | Evidência |
|---|---|
| RF-020 — Criar pedido | `POST /api/v1/orders` |
| RF-021 — Validar cliente e itens | `Order.Create` e `OrderItem.Create` |
| RF-022 — Validar produto existente e ativo | `InventoryCatalogClient` |
| RF-023 — Validar saldo livre | `OrderService.CreateAsync` |
| RF-024 — Calcular e persistir total | agregado `Order` e tabela `sales.Orders` |
| RF-025 — Consultar pedido | `GET /api/v1/orders/{id}` |
| RF-026 — Listar com paginação e filtros | `GET /api/v1/orders` |
| RF-027 — Repetição idempotente | índice único `ExternalReference` |
| RF-028 — Cancelar com concorrência | `POST /api/v1/orders/{id}/cancel` e `rowversion` |

## Capacidades entregues

- agregado `Order` com itens, total monetário e invariantes;
- fotografias de SKU e nome para preservar o histórico da venda;
- consulta síncrona ao Inventory sem referência entre assemblies;
- rejeição de produto inexistente, inativo ou sem saldo livre;
- estado inicial `PendingStock`, compatível com os eventos do próximo incremento;
- referência externa única e resposta idempotente para repetição equivalente;
- consulta por ID e listagem paginada por cliente ou situação;
- cancelamento protegido por `rowversion`; no Incremento 4, pedidos confirmados também geram compensação de estoque;
- migration SQL Server, índices, constraints e auditoria UTC;
- respostas em Problem Details e autorização `sales.user`/`admin`.

## Endpoints

| Método | Rota | Resultado principal |
|---|---|---|
| `POST` | `/api/v1/orders` | `201 Created` ou `200 OK` em repetição |
| `GET` | `/api/v1/orders` | listagem paginada |
| `GET` | `/api/v1/orders/{id}` | pedido completo |
| `POST` | `/api/v1/orders/{id}/cancel` | pedido cancelado |

## Payload de criação

```json
{
  "customerId": "2e7fb6d4-7879-4f8f-a2dc-9912a647bf42",
  "externalReference": "WEB-ORDER-0001",
  "items": [
    {
      "productId": "d748d6f1-4a4c-48d9-921f-95c8873d967d",
      "quantity": 2,
      "unitPrice": 3500.00
    }
  ]
}
```

## Consistência

A disponibilidade consultada neste incremento é apenas a pré-validação. No Incremento 4, a Outbox de Sales publica `OrderCreatedV1`; Inventory responde com `StockReservedV1` ou `StockRejectedV1`, processados por Inbox idempotente.

## Teste funcional

Com os contêineres ativos:

```powershell
.\scripts\test-increment3.ps1
```

O roteiro cria um produto, cria o pedido, confirma idempotência, consulta a listagem, verifica a rejeição por saldo insuficiente e cancela com `rowVersion`.
