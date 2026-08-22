[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Push-Location (Join-Path $PSScriptRoot '..')
try {
    docker compose --profile load run --rm load-test
    if ($LASTEXITCODE -ne 0) {
        throw "O k6 encerrou com código $LASTEXITCODE. Revise os thresholds exibidos acima."
    }
}
finally {
    Pop-Location
}
