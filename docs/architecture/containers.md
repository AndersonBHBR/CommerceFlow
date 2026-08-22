# Visão de contêineres

```mermaid
flowchart TD
    Client[Cliente] --> Gateway[API Gateway / YARP]
    Gateway --> Identity[Identity API]
    Gateway --> Sales[Sales API]
    Gateway --> Inventory[Inventory API]
    Sales --> SalesDb[(Sales DB)]
    Inventory --> InventoryDb[(Inventory DB)]
    Sales -->|pré-validação HTTP| Inventory
    Sales -->|pedido e liberação| RabbitMQ[RabbitMQ]
    RabbitMQ -->|pedido e liberação| Inventory
    Inventory -->|reserva ou rejeição| RabbitMQ
    RabbitMQ -->|reserva ou rejeição| Sales
    Gateway -.->|OTLP| Dashboard[Aspire Dashboard local]
    Identity -.->|OTLP| Dashboard
    Sales -.->|OTLP| Dashboard
    Inventory -.->|OTLP| Dashboard
```

O Gateway autentica, autoriza, limita e encaminha. Sales e Inventory validam novamente o JWT e não compartilham tabelas. A consulta HTTP é uma pré-validação rápida; a reserva definitiva ocorre por eventos. Outbox, publisher confirms, confirmação manual e Inbox fecham as janelas de falha sem prometer entrega exatamente uma vez. Em desenvolvimento, os serviços exportam traces e métricas por OTLP ao Aspire Dashboard; em produção, o destino é um OpenTelemetry Collector gerenciado.
