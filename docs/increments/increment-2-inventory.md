# Incremento 2 — Estoque

Este incremento implementa os casos de uso `UC-02`, `UC-03` e `UC-04` da especificação técnica.

## Capacidades entregues

- cadastro de produto ativo com SKU único e saldo inicial não negativo;
- consulta por ID, SKU e listagem paginada;
- atualização de nome e situação com concorrência otimista por `rowVersion`;
- entrada e saída de estoque com validação de saldo livre;
- repetição limitada de ajustes em conflito de concorrência;
- trilha imutável de ajustes com ator, motivo, referência e horário UTC;
- migration SQL Server com índices, chaves estrangeiras e `CHECK constraints`;
- leitura para usuário autenticado e escrita restrita a `inventory.manager` ou `admin`;
- respostas de erro em `application/problem+json`;
- testes unitários das invariantes centrais do produto.

## Endpoints

| Método | Rota | Política |
|---|---|---|
| `POST` | `/api/v1/products` | Estoque |
| `GET` | `/api/v1/products` | Autenticado |
| `GET` | `/api/v1/products/{id}` | Autenticado |
| `GET` | `/api/v1/products/by-sku/{sku}` | Autenticado |
| `PUT` | `/api/v1/products/{id}` | Estoque |
| `POST` | `/api/v1/products/{id}/stock-adjustments` | Estoque |
| `GET` | `/api/v1/products/{id}/stock-adjustments` | Autenticado |

## Concorrência

O SQL Server gera `RowVersion` para cada produto. Atualizações exigem a versão devolvida pela consulta anterior. Se outro processo tiver modificado o recurso, a API devolve `409 Conflict`.

Ajustes usam a versão carregada pelo EF Core e repetem a operação no máximo três vezes. Em cada tentativa, a regra de saldo livre é reavaliada. Assim, duas saídas concorrentes não podem deixar o saldo negativo.

## Respostas relevantes

- `201 Created`: produto ou ajuste criado;
- `200 OK`: consulta ou atualização concluída;
- `401 Unauthorized`: token ausente ou inválido;
- `403 Forbidden`: papel sem permissão de escrita;
- `404 Not Found`: produto inexistente;
- `409 Conflict`: SKU duplicado, saldo insuficiente ou versão desatualizada;
- `422 Unprocessable Entity`: payload inválido.

## Teste manual no PowerShell

Obtenha um token de gerente:

```powershell
$login = @{
    login = 'inventory@commerceflow.local'
    password = 'CommerceFlow#2026'
} | ConvertTo-Json

$auth = Invoke-RestMethod `
    -Method Post `
    -Uri 'http://localhost:8080/api/v1/auth/token' `
    -ContentType 'application/json' `
    -Body $login

$headers = @{ Authorization = "Bearer $($auth.accessToken)" }
```

Cadastre um produto:

```powershell
$newProduct = @{
    sku = 'NOTE-001'
    name = 'Notebook CommerceFlow'
    initialQuantity = 10
} | ConvertTo-Json

$product = Invoke-RestMethod `
    -Method Post `
    -Uri 'http://localhost:8080/api/v1/products' `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body $newProduct

$product
```

Registre uma saída de duas unidades:

```powershell
$adjustment = @{
    quantity = -2
    reason = 'Separação para pedido de demonstração'
    externalReference = 'DEMO-ORDER-001'
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:8080/api/v1/products/$($product.id)/stock-adjustments" `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body $adjustment
```

O resultado esperado é `availableQuantity = 8`, `reservedQuantity = 0` e `freeQuantity = 8`.

Para executar todo o cenário automaticamente:

```powershell
.\scripts\test-increment2.ps1
```
