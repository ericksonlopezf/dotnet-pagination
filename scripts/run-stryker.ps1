# Copyright © Erickson Lopez. MIT License.
$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  STRYKER.NET RUNNER - ERICKSONLOPEZ.PAGINATION  " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

$strykerConfig = "stryker-config.json"
if (-not (Test-Path $strykerConfig)) {
    Write-Error "stryker-config.json not found in root directory."
}

Write-Host "`n🚀 Executing Stryker for EricksonLopez.Pagination..." -ForegroundColor Yellow
dotnet stryker --config-file $strykerConfig --output StrykerOutput
$strykerExitCode = $LASTEXITCODE

if (Test-Path "scripts/record-stryker-result.js") {
    Write-Host "`n📊 Processing and recording Stryker results..." -ForegroundColor Yellow
    node scripts/record-stryker-result.js
}

if ($strykerExitCode -eq 0) {
    Write-Host "`n✅ Stryker mutation testing passed successfully." -ForegroundColor Green
} else {
    Write-Host "`n❌ Stryker mutation testing failed (Exit code: $strykerExitCode)." -ForegroundColor Red
}

exit $strykerExitCode
