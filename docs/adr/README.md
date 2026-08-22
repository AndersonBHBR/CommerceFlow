# Architecture Decision Records

As decisões completas estão na especificação técnica. O código deste incremento aplica desde já:

- ADR-001: Vendas e Estoque são capacidades e projetos independentes.
- ADR-002: o Gateway usa YARP e não contém regras de negócio.
- ADR-003: a Identity API local é substituível por um provedor OIDC.
- ADR-004: Sales e Inventory possuem conexões e bancos próprios.
- ADR-005: no Incremento 3, Sales valida disponibilidade pela API do Inventory sem compartilhar assemblies ou tabelas.
- ADR-006: eventos usam Transactional Outbox, publisher confirms, confirmação manual e Inbox idempotente.
- ADR-007: observabilidade usa OpenTelemetry e OTLP, com Aspire Dashboard apenas no ambiente local.
- ADR-008: dependências externas possuem timeout, retry, circuit breaker e prontidão explícita.
- ADR-010: APIs e eventos são versionados.
- ADR-012: Docker Compose é o ambiente de referência local.

Cada alteração arquitetural futura deve ganhar um arquivo `ADR-NNN-titulo.md` com contexto, decisão, consequências e status.
