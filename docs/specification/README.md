# Rastreabilidade da especificação

O código evolui pelos cinco incrementos definidos na especificação técnica CommerceFlow v1.0.

| Incremento | Requisitos principais | Estado |
|---|---|---|
| 1 — Fundação | RF-001 a RF-004, RF-030 e parte de RNF-001 a RNF-043 | Implementado |
| 2 — Estoque | RF-010 a RF-015 | Implementado |
| 3 — Vendas | RF-020 a RF-028 | Implementado |
| 4 — Mensageria confiável | RF-031, Outbox/Inbox e eventos v1 | Implementado |
| 5 — Engenharia de produção | RF-032, observabilidade, carga, segurança e deploy | Implementado |

## Evidências dos incrementos implementados

- autenticação e políticas: `CommerceFlow.ServiceDefaults` e `CommerceFlow.Identity.Api`;
- Gateway: `CommerceFlow.Gateway`;
- bancos independentes: `Sales.Infrastructure` e `Inventory.Infrastructure`;
- execução reproduzível: `docker-compose.yml` e `scripts/bootstrap.ps1`;
- limites automatizados: `tests/ArchitectureTests`;
- CI: `.github/workflows/ci.yml`.
- estoque: `Inventory.Domain`, `Inventory.Application` e `docs/increments/increment-2-inventory.md`;
- vendas: `Sales.Domain`, `Sales.Application` e `docs/increments/increment-3-sales.md`.
- mensageria: Outbox/Inbox, consumidores RabbitMQ e `docs/increments/increment-4-reliable-messaging.md`;
- produção: OpenTelemetry, Aspire Dashboard, k6, manifests Kubernetes e `docs/increments/increment-5-production-engineering.md`.

O documento completo permanece como artefato separado para evitar duplicação e divergência entre versões durante os primeiros commits.
