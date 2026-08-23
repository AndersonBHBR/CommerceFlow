# Runbook operacional

## Subida local

```powershell
docker compose up -d
docker compose ps
```

Espere Sales e Inventory concluírem as migrations de desenvolvimento. Em seguida, execute `scripts/test-increment5.ps1`.

O Compose também inicia o portal administrativo em `http://localhost:3000`. Depois que o contêiner `admin` estiver saudável, execute `scripts/test-increment6.ps1` para validar sessão, BFF e integrações.

## Observabilidade local

1. Gere tráfego executando o teste do Incremento 5.
2. Obtenha do log do painel a URL com token: `docker compose logs aspire-dashboard`.
3. Abra a URL informada, ou `http://localhost:18888` e forneça o token solicitado.
4. Filtre por `service.namespace=commerceflow` e acompanhe Gateway, Identity, Sales e Inventory.

O painel local não é uma solução de retenção. Em produção, encaminhe OTLP a um Collector com backend, autenticação, TLS, retenção e alertas.

## Diagnóstico rápido

| Sintoma | Verificação | Ação segura |
|---|---|---|
| Gateway responde 502/503 | `docker compose ps` e readiness dos serviços | examine logs do destino; não reinicie bancos sem preservar volumes |
| Sales readiness falha | `http://localhost:8082/health/ready` | verifique `sales-db` e autenticação RabbitMQ |
| Inventory readiness falha | `http://localhost:8083/health/ready` | verifique `inventory-db` e autenticação RabbitMQ |
| Pedido permanece `PendingStock` | filas, DLQs e logs dos consumidores | corrija a causa antes de reprocessar; preserve `MessageId` |
| DLQ cresce | RabbitMQ Management, routing key e erro estruturado | isole a mensagem, valide schema e faça replay controlado |
| Trace não aparece | variáveis `OTEL_EXPORTER_OTLP_*` e log do dashboard | valide DNS/porta 18889 dentro da rede Compose |
| Latência sobe | p95 HTTP e spans de dependências | confirme SQL, Inventory e circuit breaker antes de escalar |
| Portal não fica saudável | `docker compose logs admin` e `/api/health/live` | valide a imagem standalone, a porta 3000 e as variáveis internas dos serviços |
| Login do portal responde 503 | conectividade `admin` → `gateway:8080` | confirme o Gateway e `COMMERCEFLOW_GATEWAY_URL`; não exponha o JWT no cliente |
| Links operacionais não abrem | variáveis `*_PUBLIC_URL` do portal | use URLs acessíveis pelo navegador, diferentes das URLs internas do BFF |

## Teste de carga

```powershell
.\scripts\run-load-test.ps1
```

O cenário primeiro valida o Gateway e depois usa dez usuários virtuais por 30 segundos em leituras autenticadas diretas de Sales e Inventory. Essa separação mede os serviços sem transformar o rate limit do Gateway no gargalo artificial do ensaio. Execute em ambiente isolado e registre hardware, versão das imagens e resultados para tornar comparações válidas.

## Deploy Kubernetes

1. Publique imagens imutáveis e substitua os nomes em `deploy/kubernetes/workloads.yaml`.
2. Aponte o ConfigMap para SQL Server, RabbitMQ e OpenTelemetry Collector gerenciados.
3. Crie `commerceflow-secrets` por um cofre/operador de segredos; `secret.example.yaml` é apenas referência.
4. Execute migrations como job controlado antes de liberar a nova versão. Em `Production`, as APIs não executam migrations automaticamente.
5. Valide com `kubectl kustomize deploy/kubernetes` e aplique em um namespace de homologação.
6. Só promova após probes, smoke test, métricas e rollback terem sido verificados.

O manifesto `admin.yaml` usa cookie seguro e Ingress TLS. Substitua os domínios `.example`, a imagem e o segredo `commerceflow-portal-tls` por recursos gerenciados do ambiente antes da aplicação.

## Rollback

Faça rollback das imagens para a versão anterior imutável. Não reverta migrations destrutivas automaticamente. Eventos e contratos preservam o sufixo de versão; consumidores antigos só devem voltar se continuarem compatíveis com o schema persistido e com as filas existentes.
