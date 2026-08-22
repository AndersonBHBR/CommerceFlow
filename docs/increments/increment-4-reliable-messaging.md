# Incremento 4 — Mensageria confiável

## Objetivo

Transformar o pedido `PendingStock` em `Confirmed` ou `Rejected` por uma reserva assíncrona de Inventory, sem perder eventos entre o SQL Server e o RabbitMQ e sem aplicar duas vezes uma reentrega.

## Fluxo principal

1. Sales pré-valida o saldo por HTTP.
2. Sales grava `Order` e `OrderCreatedV1` na própria Outbox no mesmo commit.
3. O publicador envia a mensagem para `commerceflow.events` e só marca a Outbox após o confirm do broker.
4. Inventory registra a Inbox, reserva todos os itens e grava `StockReservedV1` na Outbox no mesmo commit. Se qualquer item falhar, nenhum item é reservado e `StockRejectedV1` é gravado.
5. Sales registra a Inbox e muda o pedido para `Confirmed` ou `Rejected` no mesmo commit.
6. Se um pedido confirmado ou ainda pendente for cancelado, Sales publica `StockReleaseRequestedV1`; Inventory libera reservas ativas idempotentemente.

## Topologia RabbitMQ

| Elemento | Nome | Vinculações |
|---|---|---|
| Exchange de eventos | `commerceflow.events` (`topic`, durable) | — |
| Fila de Inventory | `inventory.order-events.v1` | `sales.order.created.v1`, `sales.stock.release-requested.v1` |
| Fila de Sales | `sales.stock-results.v1` | `inventory.stock.reserved.v1`, `inventory.stock.rejected.v1` |
| Exchange de erro | `commerceflow.dead-letter` (`direct`, durable) | — |
| DLQ de Inventory | `inventory.order-events.v1.dlq` | `inventory.order-events.dead` |
| DLQ de Sales | `sales.stock-results.v1.dlq` | `sales.stock-results.dead` |

## Garantias implementadas

| Risco | Controle |
|---|---|
| Commit SQL concluído e processo cai antes de publicar | Transactional Outbox |
| Broker não roteia ou não persiste a publicação | `mandatory`, mensagens persistentes e publisher confirms |
| Consumidor cai depois do commit e antes do `ack` | Inbox com `MessageId` único |
| Concorrência entre pedidos sobre o mesmo saldo | `rowversion`, rollback e reentrega |
| Mensagem inválida ou falha repetida | DLQ após uma reentrega |
| Cancelamento durante/depois da reserva | evento compensatório de liberação |
| Diagnóstico entre serviços | `CorrelationId`, `MessageId`, tipo e routing key |

## Estados do pedido

| Evento/comando | Estado anterior | Estado final |
|---|---|---|
| criação | — | `PendingStock` |
| `StockReservedV1` | `PendingStock` | `Confirmed` |
| `StockRejectedV1` | `PendingStock` | `Rejected` |
| cancelamento | `PendingStock` ou `Confirmed` | `Cancelled` |
| resposta tardia após cancelamento | `Cancelled` | `Cancelled` (Inbox registrada, sem regressão) |

## Critérios de aceite

- Pedido válido chega a `Confirmed` e aumenta `ReservedQuantity` sem alterar `AvailableQuantity`.
- Repetição da criação com a mesma referência não cria outro pedido nem outra reserva.
- Cancelamento chega a `Cancelled` e devolve o saldo reservado.
- Alteração de domínio e Outbox são atômicas em Sales e Inventory.
- O consumidor só confirma a entrega depois de persistir Inbox e efeitos.
- Reentrega do mesmo `MessageId` não reaplica o efeito.
- Mensagens venenosas são encaminhadas à DLQ.
- Os contratos são versionados com sufixo `V1` e não referenciam implementações dos serviços.

## Teste funcional

Com o ambiente ativo:

```powershell
.\scripts\test-increment4.ps1
```

O resultado esperado é `10/10 - Teste funcional do Incremento 4 concluído com sucesso.` em verde.
