# Incremento 5 — Engenharia de produção

## Objetivo

Tornar o CommerceFlow observável, resiliente, verificável sob carga e preparado para uma plataforma de contêineres, preservando os fluxos funcionais dos quatro incrementos anteriores.

## Entregas

| Pilar | Evidência |
|---|---|
| Observabilidade | OpenTelemetry em todas as APIs, traces HTTP, métricas HTTP, spans e contadores de mensageria, Aspire Dashboard local |
| Resiliência | `HttpClientFactory`, timeout, retry, circuit breaker, retentativa SQL e reconexão RabbitMQ |
| Operação | liveness separado de readiness, banco e broker na prontidão, logs JSON e runbook |
| Segurança | JWT e papéis, rate limiting, limite de corpo, cabeçalhos defensivos, remoção de `Server`, TLS opcional no RabbitMQ, contêiner não root |
| Qualidade | build com warnings como erro, testes, audit de NuGet, imagens Docker no CI e Dependabot |
| Carga | cenário k6 autenticado com thresholds objetivos |
| Deploy | manifests Kubernetes com probes, recursos, réplicas, segredos externos e `securityContext` restritivo |

## Critérios de aceite

- Gateway, Identity, Sales e Inventory publicam traces e métricas por OTLP.
- O fluxo funcional de criação, reserva, confirmação, cancelamento e compensação continua aprovado.
- `/health/ready` de Sales e Inventory falha se seu SQL Server ou RabbitMQ não estiver acessível.
- A chamada Sales → Inventory possui timeout total, timeout por tentativa, retry limitado e circuit breaker.
- As respostas carregam os cabeçalhos defensivos e não divulgam o servidor HTTP.
- O cenário k6 mantém menos de 1% de falhas, mais de 99% de checks aprovados e p95 abaixo de 750 ms no ambiente de referência.
- O CI restaura com audit, compila, testa, valida Compose e constrói as quatro imagens.
- Os manifests não embutem credenciais reais e executam aplicações sem privilégios.

## Validação funcional

Com os contêineres em execução:

```powershell
.\scripts\test-increment5.ps1
```

Para incluir o teste de carga no mesmo comando:

```powershell
.\scripts\test-increment5.ps1 -RunLoadTest
```

O resultado funcional esperado é `12/12 - Teste funcional do Incremento 5 concluído com sucesso.` em verde. O k6 falha o processo quando qualquer threshold não é atendido.
