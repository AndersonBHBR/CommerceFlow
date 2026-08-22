# ADR-006 — Outbox, Inbox e entrega pelo menos uma vez

- Status: aceita
- Data: 2026-08-21

## Contexto

Sales precisa publicar pedidos depois do commit do próprio banco, e Inventory precisa responder depois de reservar ou rejeitar o estoque. Fazer commit SQL e publicar no RabbitMQ como duas ações independentes cria janelas de perda. O broker também pode reenviar uma entrega quando não recebe a confirmação do consumidor.

## Decisão

- A alteração de domínio e a mensagem de saída são persistidas na mesma transação SQL.
- Um `BackgroundService` lê a Outbox e publica mensagens persistentes com `mandatory` e publisher confirms.
- O consumidor usa confirmação manual e somente envia `ack` depois do commit SQL.
- O identificador do evento é chave primária da Inbox; reentregas já processadas viram no-op.
- A primeira falha de processamento é reenfileirada. Uma segunda entrega com falha é rejeitada para a DLQ.
- Cancelamentos publicam uma solicitação compensatória de liberação de estoque.

## Consequências

O fluxo oferece entrega pelo menos uma vez e processamento de efeito único por consumidor, mas não “exactly once” distribuído. A latência passa a ser eventual e as tabelas Outbox/Inbox exigem retenção operacional futura. Mensagens em DLQ precisam de diagnóstico e reprocessamento consciente.
