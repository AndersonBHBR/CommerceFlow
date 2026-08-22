[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:8080'
)

$ErrorActionPreference = 'Stop'

Write-Host '1/6 - Obtendo token de inventory.manager...'
$loginBody = @{
    login = 'inventory@commerceflow.local'
    password = 'CommerceFlow#2026'
} | ConvertTo-Json

$auth = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/auth/token" `
    -ContentType 'application/json' `
    -Body $loginBody

$headers = @{ Authorization = "Bearer $($auth.accessToken)" }

Write-Host '2/6 - Cadastrando produto com saldo 10...'
$sku = 'NOTE-' + (Get-Date -Format 'yyyyMMddHHmmssfff')
$productBody = @{
    sku = $sku
    name = 'Notebook CommerceFlow'
    initialQuantity = 10
} | ConvertTo-Json

$product = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/products" `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body $productBody

if ($product.availableQuantity -ne 10 -or $product.freeQuantity -ne 10) {
    throw 'O cadastro nao devolveu o saldo inicial esperado.'
}

Write-Host '3/6 - Atualizando produto com rowVersion...'
$updateBody = @{
    name = 'Notebook CommerceFlow Atualizado'
    isActive = $true
    rowVersion = $product.rowVersion
} | ConvertTo-Json

$product = Invoke-RestMethod `
    -Method Put `
    -Uri "$BaseUrl/api/v1/products/$($product.id)" `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body $updateBody

if ($product.name -ne 'Notebook CommerceFlow Atualizado') {
    throw 'A atualizacao do produto nao devolveu o nome esperado.'
}

Write-Host '4/6 - Registrando saida de 2 unidades...'
$adjustmentBody = @{
    quantity = -2
    reason = 'Teste automatizado do Incremento 2'
    externalReference = 'TEST-INCREMENT-2'
} | ConvertTo-Json

$adjustment = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/v1/products/$($product.id)/stock-adjustments" `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body $adjustmentBody

if ($adjustment.product.availableQuantity -ne 8 -or $adjustment.product.freeQuantity -ne 8) {
    throw 'O ajuste nao devolveu o saldo esperado.'
}

Write-Host '5/6 - Consultando produto e trilha de ajustes...'
$currentProduct = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/v1/products/$($product.id)" `
    -Headers $headers

$history = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/v1/products/$($product.id)/stock-adjustments?page=1&pageSize=20" `
    -Headers $headers

if ($currentProduct.freeQuantity -ne 8) {
    throw 'A consulta nao encontrou o saldo esperado.'
}

if ($history.totalCount -lt 2) {
    throw 'A trilha deveria conter o saldo inicial e a saida de estoque.'
}

Write-Host '6/6 - Teste funcional concluido com sucesso.' -ForegroundColor Green
[PSCustomObject]@{
    ProductId = $currentProduct.id
    Sku = $currentProduct.sku
    AvailableQuantity = $currentProduct.availableQuantity
    ReservedQuantity = $currentProduct.reservedQuantity
    FreeQuantity = $currentProduct.freeQuantity
    AdjustmentCount = $history.totalCount
    RowVersion = $currentProduct.rowVersion
}
