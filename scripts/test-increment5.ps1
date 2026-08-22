[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:8080',
    [int]$TimeoutSeconds = 120,
    [switch]$RunLoadTest
)

$ErrorActionPreference = 'Stop'

function Wait-Healthy([string]$Url) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 5
            if ([int]$response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timeout aguardando HTTP 200 em $Url."
}

function Get-HttpResponse([string]$Url) {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    try {
        return $client.GetAsync($Url).GetAwaiter().GetResult()
    }
    finally {
        $client.Dispose()
        $handler.Dispose()
    }
}

Write-Host '1/12 - Validando liveness do Gateway...'
Wait-Healthy "$BaseUrl/health/live"

Write-Host '2/12 - Validando readiness do Identity...'
Wait-Healthy 'http://localhost:8081/health/ready'

Write-Host '3/12 - Validando Sales, SQL Server e RabbitMQ...'
Wait-Healthy 'http://localhost:8082/health/ready'

Write-Host '4/12 - Validando Inventory, SQL Server e RabbitMQ...'
Wait-Healthy 'http://localhost:8083/health/ready'

Write-Host '5/12 - Conferindo cabeçalhos defensivos...'
$gatewayResponse = Get-HttpResponse "$BaseUrl/"
if (-not $gatewayResponse.Headers.Contains('X-Content-Type-Options')) {
    throw 'O cabeçalho X-Content-Type-Options não foi encontrado.'
}
if (-not $gatewayResponse.Headers.Contains('Content-Security-Policy')) {
    throw 'O cabeçalho Content-Security-Policy não foi encontrado.'
}

Write-Host '6/12 - Confirmando a remoção do cabeçalho Server...'
if ($gatewayResponse.Headers.Server.Count -gt 0) {
    throw "O cabeçalho Server ainda está exposto: $($gatewayResponse.Headers.Server)."
}
$gatewayResponse.Dispose()

Write-Host '7/12 - Confirmando proteção de rota sem JWT...'
$unauthorized = Get-HttpResponse "$BaseUrl/api/v1/orders?page=1&pageSize=1"
if ([int]$unauthorized.StatusCode -ne 401) {
    throw "A rota de pedidos respondeu $([int]$unauthorized.StatusCode), mas deveria responder 401."
}
$unauthorized.Dispose()

Write-Host '8/12 - Executando o fluxo cumulativo de mensageria confiável...'
& (Join-Path $PSScriptRoot 'test-increment4.ps1') `
    -BaseUrl $BaseUrl `
    -TimeoutSeconds $TimeoutSeconds | Out-Host

Write-Host '9/12 - Confirmando o contêiner do painel de observabilidade...'
$runningServices = docker compose ps --services --status running
if ($LASTEXITCODE -ne 0 -or $runningServices -notcontains 'aspire-dashboard') {
    throw 'O contêiner aspire-dashboard não está em execução.'
}

Write-Host '10/12 - Validando o endpoint web do Aspire Dashboard...'
$dashboard = Get-HttpResponse 'http://localhost:18888/'
if ([int]$dashboard.StatusCode -notin @(200, 302, 307, 401)) {
    throw "Aspire Dashboard respondeu com HTTP $([int]$dashboard.StatusCode)."
}
$dashboard.Dispose()

Write-Host '11/12 - Validando o perfil reproduzível de teste de carga...'
$profileServices = docker compose --profile load config --services
if ($LASTEXITCODE -ne 0 -or $profileServices -notcontains 'load-test') {
    throw 'O serviço load-test não foi encontrado no perfil load.'
}

if ($RunLoadTest) {
    Write-Host 'Executando também o cenário k6 solicitado...'
    & (Join-Path $PSScriptRoot 'run-load-test.ps1')
}

Write-Host '12/12 - Teste funcional do Incremento 5 concluído com sucesso.' -ForegroundColor Green
[PSCustomObject]@{
    Gateway = $BaseUrl
    Observability = 'OpenTelemetry + Aspire Dashboard'
    Resilience = 'Timeout + retry + circuit breaker + readiness'
    Security = 'JWT + rate limiting + headers + non-root containers'
    LoadTest = if ($RunLoadTest) { 'k6 aprovado' } else { 'k6 disponível sob demanda' }
}
