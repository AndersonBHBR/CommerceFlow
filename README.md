# CommerceFlow

Assessment de microsserviços em .NET 10, criado como projeto de portólio independente para demonstrar arquitetura, segurança, consistência eventual e engenharia de produção.

## Estado atual

Este repositório contém os **Incrementos 1 - Fundação, 2 - Estoque, 3 - Vendas, 4 - Mensageria confiável e 5 - Engenharia de produção**:

- solução `.slnx`, build determiní­stico e gerenciamento central de pacotes;
- API Gateway com YARP, JWT, autorização por rota, rate limiting e correlação;
- Identity API substituÃ­vel, com usuários sintáticos e senhas persistidas por PBKDF2;
- limites de Sales e Inventory em Domain, Application, Infrastructure e API;
- SQL Server isolado por serviço e RabbitMQ provisionado pelo Compose;
- liveness, readiness, Problem Details e logs JSON estruturados;
- OpenAPI e testes de arquitetura;
- pipeline de build e testes.
- domínio de produto e ajuste de estoque com invariantes explícitas;
- endpoints de cadastro, consulta, atualização e ajustes;
- paginação, auditoria e erros HTTP padronizados;
- concorrência otimista por `rowversion`, repetição limitada e proteção contra saldo negativo;
- migration SQL Server e testes das regras centrais.
- agregado de pedido com itens, total, estados e auditoria;
- criação idempotente, consulta, paginação, filtros e cancelamento de pedidos;
- validação de produto, atividade e saldo livre pela API do Inventory;
- migration do banco Sales e concorrência otimista por `rowversion`.
- eventos `OrderCreatedV1`, `StockReservedV1`, `StockRejectedV1` e compensação de cancelamento;
- Transactional Outbox nos dois serviços e Inbox idempotente nos consumidores;
- publicação persistente com `mandatory`, publisher confirms e retentativa exponencial;
- consumo com confirmação manual, prefetch 1, uma retentativa e Dead Letter Queue;
- reserva atômica, rejeição por regra de estoque e liberação compensatória;
- correlação ponta a ponta e migrations das estruturas de mensageria.
- traces e métricas OpenTelemetry exportados por OTLP;
- Aspire Dashboard local para inspeção do fluxo distribuído;
- resiliência HTTP com timeout, retry e circuit breaker;
- prontidão que valida SQL Server e RabbitMQ;
- cabeçalhos defensivos, limite de corpo e remoção do cabeçalho `Server`;
- teste de carga k6 com thresholds objetivos;
- audit de dependÃªncias, build de imagens no CI e atualizações automatizadas;
- manifests Kubernetes endurecidos e runbook operacional.

## Pré-requisitos

- Docker Desktop com Docker Compose; ou
- .NET SDK 10 e duas instâncias acessí­veis de SQL Server.

## Subir todo o ambiente no Windows

No PowerShell, a partir da raiz:

```powershell
.\scripts\bootstrap.ps1 -Start
```

O script cria `.env` com segredos locais aleatórios, constrói as imagens e inicia os contêineres.

## Endereços locais

| Componente | URL/porta |
|---|---|
| Gateway | `http://localhost:8080` |
| Identity direta | `http://localhost:8081` |
| Sales direta | `http://localhost:8082` |
| Inventory direta | `http://localhost:8083` |
| RabbitMQ Management | `http://localhost:15672` |
| Aspire Dashboard | `http://localhost:18888` |
| Sales SQL Server | `localhost,14331` |
| Inventory SQL Server | `localhost,14332` |

Health checks: `/health/live` e `/health/ready`. OpenAPI: `/openapi/v1.json`.

## Obter um token

Os três usuários de demonstração utilizam a senha sintática `CommerceFlow#2026`:

| Login | Papel |
|---|---|
| `sales@commerceflow.local` | `sales.user` |
| `inventory@commerceflow.local` | `inventory.manager` |
| `admin@commerceflow.local` | `admin` |

```powershell
$body = @{ login = 'sales@commerceflow.local'; password = 'CommerceFlow#2026' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri 'http://localhost:8080/api/v1/auth/token' -ContentType 'application/json' -Body $body
```

Os hashes e salts PBKDF2 ficam no arquivo de configuração de desenvolvimento; a senha não é persistida pela aplicação.

## Desenvolvimento sem Docker

```powershell
dotnet restore CommerceFlow.slnx
dotnet build CommerceFlow.slnx
dotnet test CommerceFlow.slnx
```

Defina `Jwt__SigningKey` e as connection strings por User Secrets ou variáveis de ambiente. Nunca versione `.env`, credenciais reais ou dados corporativos.

## Limites arquiteturais

- `Domain` não referencia Application, Infrastructure, APIs ou outro domí­nio.
- `Application` referencia apenas o próprio Domain e contratos necessários.
- `Infrastructure` implementa persistência e integrações externas.
- `API` é a composition root do serviço.
- `Contracts` contém DTOs de integração, sem regras internas de domí­nio.

Veja `docs/architecture` e `docs/adr` para a documentação versionada.

Os roteiros estão em `docs/increments`.

Com os contêineres ativos, valide o incremento inteiro com:

```powershell
.\scripts\test-increment2.ps1
.\scripts\test-increment3.ps1
.\scripts\test-increment4.ps1
.\scripts\test-increment5.ps1
```

O teste do Incremento 5 reaproveita o fluxo cumulativo do Incremento 4 e acrescenta readiness, segurança e observabilidade. Para executar o cenário k6 separadamente:

```powershell
.\scripts\run-load-test.ps1
```

O Aspire Dashboard mantém autenticação local. Use `docker compose logs aspire-dashboard` para localizar a URL com token de acesso. Consulte `docs/operations/runbook.md` para diagnóstico e deploy.

## 🙋 Sobre o Autor

Feito com 💻 e ☕ por [Anderson Lima Araújo](https://www.linkedin.com/in/anderson-araujo-pcd)😊
Sou desenvolvedor Full Stack com foco em IA, APIs modernas, soluções web escaláveis e interesse em projetos internacionais 🌍
<p>
    <img align=left margin=10 width=80 src="https://avatars.githubusercontent.com/u/7528140?v=4"/>
    <p>&nbsp&nbsp&nbspAnderson Lima Araújo<br>
    &nbsp&nbsp&nbsp<a href="http://instagram.com/andersonbhbr">Instagram</a>&nbsp;|&nbsp;<a href="https://github.com/AndersonBHBR">GitHub</a>&nbsp;|&nbsp;<a href="https://www.linkedin.com/in/anderson-araujo-pcd/">LinkedIn</a>&nbsp;|&nbsp;<a href="https://www.behance.net/andersonbhbr">Behance</a></p>
</p>
<br/><br/>
