# ADR-007 — OpenTelemetry e OTLP

## Status

Aceita.

## Contexto

Logs isolados não reconstroem uma chamada que atravessa Gateway, Sales e Inventory, nem tornam latência e taxa de erro mensuráveis de forma padronizada.

## Decisão

Todas as APIs produzem traces e métricas OpenTelemetry para um endpoint OTLP configurável. HTTP de entrada, HTTP de saída e os trabalhos de mensageria recebem instrumentação. O `CorrelationId` continua sendo o identificador funcional entre mensagens assíncronas.

O Compose inicia um Aspire Dashboard standalone para desenvolvimento. O painel mantém autenticação própria e somente sua porta web é publicada no host. Em produção, os manifests apontam para um OpenTelemetry Collector externo; o Dashboard local não é tratado como armazenamento durável.

## Consequências

- o fluxo HTTP pode ser acompanhado entre os serviços;
- publicações e consumos expõem spans e contadores com tags de baixa cardinalidade;
- o backend de observabilidade pode ser trocado sem alterar o domínio;
- a retenção e os alertas continuam sendo responsabilidade da plataforma de produção.
