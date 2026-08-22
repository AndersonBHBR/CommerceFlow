# ADR-008 — Resiliência e prontidão

## Status

Aceita.

## Contexto

Uma falha transitória no Inventory não deve bloquear indefinidamente a criação de pedidos. Ao mesmo tempo, retry indiscriminado pode ampliar uma indisponibilidade. Liveness não pode afirmar que uma instância está pronta para receber tráfego quando banco ou broker estão inacessíveis.

## Decisão

O cliente HTTP tipado de Sales usa a pipeline padrão de resiliência do .NET, configurada com timeout total de 10 segundos, timeout de tentativa de 3 segundos, duas retentativas e circuit breaker. SQL Server mantém retentativa limitada do provider. `/health/live` verifica o processo; `/health/ready` verifica também o banco do serviço e uma conexão autenticada com RabbitMQ.

## Consequências

- falhas curtas podem ser absorvidas sem espera infinita;
- o circuit breaker reduz pressão sobre Inventory durante falhas repetidas;
- orquestradores podem retirar do tráfego uma instância sem reiniciá-la por qualquer dependência degradada;
- métricas e traces devem ser usados para ajustar os limites com dados reais.
