[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:8080',
    [int]$TimeoutSeconds = 90
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

function Wait-OrderStatus([Guid]$OrderId, [string]$ExpectedStatus, [hashtable]$Headers) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $order = Invoke-RestMethod `
            -Method Get `
            -Uri "$BaseUrl/api/v1/orders/$OrderId" `
            -Headers $Headers

        if ($order.status -eq $ExpectedStatus) {
            return $order
        }

        Start-Sleep -Milliseconds 750
    } while ((Get-Date) -lt $deadline)

    throw "Timeout aguardando pedido $OrderId ficar em $ExpectedStatus. Último estado: $($order.status)."
}

function Wait-ProductBalance(
    [Guid]$ProductId,
    [int]$ExpectedReserved,
    [int]$ExpectedFree,
    [hashtable]$Headers) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $product = Invoke-RestMethod `
            -Method Get `
            -Uri "$BaseUrl/api/v1/products/$ProductId" `
            -Headers $Headers

        if (
            [int]$product.reservedQuantity -eq $ExpectedReserved -and
            [int]$product.freeQuantity -eq $ExpectedFree) {
            return $product
        }

        Start-Sleep -Milliseconds 750
    } while ((Get-Date) -lt $deadline)

    throw "Timeout aguardando saldo reservado=$ExpectedReserved e livre=$ExpectedFree. " +
        "Último saldo: reservado=$($product.reservedQuantity), livre=$($product.freeQuantity)."
}

Write-Host '1/10 - Obtendo tokens de Estoque e Vendas...'
$inventoryHeaders = @{ Authorization = "Bearer $(Get-CommerceFlowToken 'inventory@commerceflow.local')" }
$salesHeaders = @{ Authorization = "Bearer $(Get-CommerceFlowToken 'sales@commerceflow.local')" }

Write-Host '2/10 - Cadastrando produto com cinco unidades...'
$suffix = Get-Date -Format 'yyyyMMddHHmmssfff'
$productBody = @{
    sku = "MSG-$suffix"
    name = 'Produto do teste de Mensageria'
    initialQuantity = 5
} | ConvertTo-Json
$product = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/products" `
    -Headers $inventoryHeaders `
    -ContentType 'application/json' `
    -Body $productBody

Write-Host '3/10 - Criando pedido que gera OrderCreatedV1 pela Outbox...'
$customerId = [Guid]::NewGuid()
$orderBody = @{
    customerId = $customerId
    externalReference = "TEST-MSG-$suffix"
    items = @(
        @{
            productId = $product.id
            quantity = 2
            unitPrice = 125.50
        }
    )
} | ConvertTo-Json -Depth 5
$createdOrder = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/orders" `
    -Headers $salesHeaders `
    -ContentType 'application/json' `
    -Body $orderBody

Write-Host '4/10 - Aguardando StockReservedV1 confirmar o pedido...'
$confirmedOrder = Wait-OrderStatus $createdOrder.id 'Confirmed' $salesHeaders

Write-Host '5/10 - Validando a reserva atômica no Estoque...'
$reservedProduct = Wait-ProductBalance $product.id 2 3 $inventoryHeaders

Write-Host '6/10 - Repetindo a criação para validar idempotência de negócio...'
$replayedOrder = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/orders" `
    -Headers $salesHeaders `
    -ContentType 'application/json' `
    -Body $orderBody
if ($replayedOrder.id -ne $confirmedOrder.id) {
    throw 'A repetição idempotente criou outro pedido.'
}
Start-Sleep -Seconds 2
$unchangedProduct = Wait-ProductBalance $product.id 2 3 $inventoryHeaders

Write-Host '7/10 - Cancelando pedido confirmado e gerando compensação...'
$cancelBody = @{ rowVersion = $confirmedOrder.rowVersion } | ConvertTo-Json
$cancelledOrder = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/orders/$($confirmedOrder.id)/cancel" `
    -Headers $salesHeaders `
    -ContentType 'application/json' `
    -Body $cancelBody
if ($cancelledOrder.status -ne 'Cancelled') {
    throw 'O pedido não foi cancelado.'
}

Write-Host '8/10 - Aguardando a liberação assíncrona da reserva...'
$releasedProduct = Wait-ProductBalance $product.id 0 5 $inventoryHeaders

Write-Host '9/10 - Confirmando que todo o saldo voltou a ficar disponível...'
if (
    [int]$releasedProduct.availableQuantity -ne 5 -or
    [int]$releasedProduct.reservedQuantity -ne 0 -or
    [int]$releasedProduct.freeQuantity -ne 5) {
    throw 'A compensação deixou o saldo do produto inconsistente.'
}

Write-Host '10/10 - Teste funcional do Incremento 4 concluído com sucesso.' -ForegroundColor Green
[PSCustomObject]@{
    OrderId = $cancelledOrder.id
    OrderStatus = $cancelledOrder.status
    ProductId = $releasedProduct.id
    ProductSku = $releasedProduct.sku
    AvailableQuantity = $releasedProduct.availableQuantity
    ReservedQuantity = $releasedProduct.reservedQuantity
    FreeQuantity = $releasedProduct.freeQuantity
    Guarantees = 'Outbox + publisher confirms + manual ack + Inbox + compensação'
}
