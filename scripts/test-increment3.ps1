[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:8080'
)

$ErrorActionPreference = 'Stop'

function Get-CommerceFlowToken([string]$Login) {
    $body = @{
        login = $Login
        password = 'CommerceFlow#2026'
    } | ConvertTo-Json

    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/v1/auth/token" `
        -ContentType 'application/json' `
        -Body $body

    return $response.accessToken
}

Write-Host '1/8 - Obtendo tokens de Estoque e Vendas...'
$inventoryHeaders = @{ Authorization = "Bearer $(Get-CommerceFlowToken 'inventory@commerceflow.local')" }
$salesHeaders = @{ Authorization = "Bearer $(Get-CommerceFlowToken 'sales@commerceflow.local')" }

Write-Host '2/8 - Cadastrando produto com saldo livre 5...'
$suffix = Get-Date -Format 'yyyyMMddHHmmssfff'
$productBody = @{
    sku = "SALE-$suffix"
    name = 'Produto do teste de Vendas'
    initialQuantity = 5
} | ConvertTo-Json

$product = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/products" `
    -Headers $inventoryHeaders `
    -ContentType 'application/json' `
    -Body $productBody

Write-Host '3/8 - Criando pedido e validando o saldo no Inventory...'
$customerId = [Guid]::NewGuid()
$orderBody = @{
    customerId = $customerId
    externalReference = "TEST-SALES-$suffix"
    items = @(
        @{
            productId = $product.id
            quantity = 2
            unitPrice = 3500.00
        }
    )
} | ConvertTo-Json -Depth 5

$order = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/orders" `
    -Headers $salesHeaders `
    -ContentType 'application/json' `
    -Body $orderBody

if ($order.status -ne 'PendingStock' -or [decimal]$order.totalAmount -ne 7000) {
    throw 'O pedido não foi criado com situação ou total esperados.'
}

Write-Host '4/8 - Repetindo a referência externa para validar idempotência...'
$replayedOrder = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/orders" `
    -Headers $salesHeaders `
    -ContentType 'application/json' `
    -Body $orderBody

if ($replayedOrder.id -ne $order.id) {
    throw 'A repetição idempotente criou outro pedido.'
}

Write-Host '5/8 - Consultando o pedido e a listagem paginada...'
$currentOrder = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/v1/orders/$($order.id)" `
    -Headers $salesHeaders

$orders = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/v1/orders?page=1&pageSize=20&customerId=$customerId" `
    -Headers $salesHeaders

if ($currentOrder.id -ne $order.id -or $orders.totalCount -lt 1) {
    throw 'A consulta de pedidos não devolveu o registro esperado.'
}

Write-Host '6/8 - Rejeitando pedido com quantidade superior ao saldo livre...'
$insufficientBody = @{
    customerId = $customerId
    externalReference = "TEST-NO-STOCK-$suffix"
    items = @(
        @{
            productId = $product.id
            quantity = 6
            unitPrice = 100.00
        }
    )
} | ConvertTo-Json -Depth 5

$conflictReceived = $false
try {
    Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/v1/orders" `
        -Headers $salesHeaders `
        -ContentType 'application/json' `
        -Body $insufficientBody
}
catch {
    $statusCode = [int]$_.Exception.Response.StatusCode
    if ($statusCode -ne 409) {
        throw
    }

    $conflictReceived = $true
}

if (-not $conflictReceived) {
    throw 'O pedido sem saldo deveria ter sido rejeitado com HTTP 409.'
}

Write-Host '7/8 - Cancelando pedido com concorrência otimista...'
$cancelBody = @{
    rowVersion = $currentOrder.rowVersion
} | ConvertTo-Json

$cancelledOrder = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/orders/$($order.id)/cancel" `
    -Headers $salesHeaders `
    -ContentType 'application/json' `
    -Body $cancelBody

if ($cancelledOrder.status -ne 'Cancelled') {
    throw 'O pedido não foi cancelado.'
}

Write-Host '8/8 - Teste funcional do Incremento 3 concluído com sucesso.' -ForegroundColor Green
[PSCustomObject]@{
    OrderId = $cancelledOrder.id
    Number = $cancelledOrder.number
    ExternalReference = $cancelledOrder.externalReference
    Status = $cancelledOrder.status
    TotalAmount = $cancelledOrder.totalAmount
    ProductId = $product.id
    ProductSku = $product.sku
}
