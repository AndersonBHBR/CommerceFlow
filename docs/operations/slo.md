# SLOs iniciais

Estes objetivos são um ponto de partida para homologação; devem ser recalibrados com tráfego e capacidade reais.

| Indicador | Objetivo | Janela |
|---|---|---|
| Disponibilidade do Gateway | 99,9% de respostas não 5xx | 30 dias |
| Latência de leitura | p95 menor que 500 ms | 30 minutos |
| Erros no teste de carga | menos de 1% | execução |
| Processamento assíncrono | p95 menor que 30 s entre Outbox e Inbox | 30 minutos |
| DLQ | zero mensagem nova não investigada | 15 minutos |

## Alertas mínimos

- queima rápida do orçamento de erro em 5 e 30 minutos;
- readiness indisponível por mais de 5 minutos;
- circuit breaker aberto repetidamente;
- Outbox pendente acima de 30 segundos;
- qualquer crescimento de DLQ;
- p95 acima do objetivo em três janelas consecutivas.
