[CmdletBinding()]
param(
    [string]$PortalUrl = 'http://localhost:3000',
    [int]$TimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$PortalUrl = $PortalUrl.TrimEnd('/')

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

Write-Host '1/10 - Validando liveness do portal...'
Wait-Healthy "$PortalUrl/api/health/live"

Write-Host '2/10 - Conferindo cabecalhos defensivos...'
$loginResponse = Get-HttpResponse "$PortalUrl/login"
if (-not $loginResponse.Headers.Contains('X-Content-Type-Options')) {
    throw 'O cabecalho X-Content-Type-Options nao foi encontrado.'
}
if (-not $loginResponse.Headers.Contains('Content-Security-Policy')) {
    throw 'O cabecalho Content-Security-Policy nao foi encontrado.'
}
if ($loginResponse.Headers.Contains('X-Powered-By')) {
    throw 'O cabecalho X-Powered-By nao deveria ser divulgado.'
}
$loginResponse.Dispose()

Write-Host '3/10 - Confirmando protecao da observabilidade sem sessao...'
$unauthorized = Get-HttpResponse "$PortalUrl/api/observability/health"
if ([int]$unauthorized.StatusCode -ne 401) {
    throw "A API protegida respondeu $([int]$unauthorized.StatusCode), mas deveria responder 401."
}
$unauthorized.Dispose()

Write-Host '4/10 - Autenticando pelo BFF...'
$webSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginBody = @{
    login = 'admin@commerceflow.local'
    password = 'CommerceFlow#2026'
} | ConvertTo-Json
$login = Invoke-WebRequest `
    -UseBasicParsing `
    -Method Post `
    -Uri "$PortalUrl/api/session" `
    -ContentType 'application/json' `
    -Body $loginBody `
    -WebSession $webSession `
    -TimeoutSec 10
if ([int]$login.StatusCode -ne 200) {
    throw "O login respondeu HTTP $([int]$login.StatusCode)."
}

Write-Host '5/10 - Validando o cookie HttpOnly...'
$portalUri = [Uri]$PortalUrl
$sessionCookie = $webSession.Cookies.GetCookies($portalUri) |
    Where-Object { $_.Name -eq 'commerceflow_session' } |
    Select-Object -First 1
if ($null -eq $sessionCookie -or -not $sessionCookie.HttpOnly) {
    throw 'O cookie de sessao HttpOnly nao foi encontrado.'
}

Write-Host '6/10 - Validando o centro de observabilidade...'
$snapshot = $null
$affected = @()
$observabilityDeadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    try {
        $snapshot = Invoke-RestMethod `
            -Method Get `
            -Uri "$PortalUrl/api/observability/health" `
            -WebSession $webSession `
            -TimeoutSec 15
        $affected = @($snapshot.services | Where-Object { $_.state -ne 'operational' })
        if (@($snapshot.services).Count -eq 4 -and $affected.Count -eq 0) {
            break
        }
    }
    catch {
        $snapshot = $null
    }
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $observabilityDeadline)

if ($null -eq $snapshot -or @($snapshot.services).Count -ne 4) {
    throw 'A observabilidade nao retornou os quatro servicos esperados.'
}
if ($affected.Count -gt 0) {
    throw "Ha servicos nao operacionais: $($affected.name -join ', ')."
}

Write-Host '7/10 - Validando o BFF de Estoque...'
$inventory = Invoke-RestMethod `
    -Method Get `
    -Uri "$PortalUrl/api/inventory/products?page=1&pageSize=1" `
    -WebSession $webSession `
    -TimeoutSec 15
if ($null -eq $inventory.items) {
    throw 'O BFF de Estoque nao retornou a colecao esperada.'
}

Write-Host '8/10 - Validando o BFF de Vendas...'
$sales = Invoke-RestMethod `
    -Method Get `
    -Uri "$PortalUrl/api/sales/orders?page=1&pageSize=1" `
    -WebSession $webSession `
    -TimeoutSec 15
if ($null -eq $sales.items) {
    throw 'O BFF de Vendas nao retornou a colecao esperada.'
}

Write-Host '9/10 - Confirmando o conteiner do portal...'
$runningServices = docker compose ps --services --status running
if ($LASTEXITCODE -ne 0 -or $runningServices -notcontains 'admin') {
    throw 'O conteiner admin nao esta em execucao.'
}

Write-Host '10/10 - Teste funcional do Incremento 6 concluido com sucesso.' -ForegroundColor Green
[PSCustomObject]@{
    Portal = $PortalUrl
    Services = @($snapshot.services).Count
    Session = 'JWT protegido em cookie HttpOnly'
    InventoryBff = 'Aprovado'
    SalesBff = 'Aprovado'
    ObservabilityBff = 'Aprovado'
}
