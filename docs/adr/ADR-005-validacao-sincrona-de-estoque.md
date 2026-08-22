# ADR-005 — Validação síncrona de disponibilidade no Incremento 3

- **Status:** aceito como decisão incremental
- **Data:** 2026-08-20

## Contexto

O serviço de Vendas precisa impedir a criação de pedidos para produtos inexistentes, inativos ou sem saldo livre. Os bancos de Sales e Inventory não podem ser compartilhados. A reserva definitiva e as respostas `StockReservedV1` e `StockRejectedV1` pertencem ao Incremento 4, que introduziu Outbox/Inbox e entrega confiável pelo RabbitMQ.

## Decisão

Durante o Incremento 3, Sales consulta a API do Inventory por HTTP usando o token JWT recebido na requisição. O cliente usa apenas o contrato JSON público do Inventory e não referencia nenhum assembly do outro serviço. Depois da validação, o pedido é persistido como `PendingStock`.

## Consequências

- o limite de cada microsserviço e seu banco independente são preservados;
- o usuário recebe falha imediata para produto inválido ou saldo insuficiente;
- a referência externa oferece repetição idempotente;
- a consulta representa uma fotografia e não uma reserva atômica: outra operação pode alterar o saldo logo depois;
- o Incremento 4 fecha essa lacuna com `OrderCreatedV1`, reserva no Inventory e retorno confiável de aceite ou rejeição.
