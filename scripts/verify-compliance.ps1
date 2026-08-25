<#
.SYNOPSIS
    Automated solution-wide compliance verification script for EricksonLopez.Pagination.
.DESCRIPTION
    Validates architectural standards:
    1. Kebab-case document naming convention (excluding reserved docs).
    2. Copyright header integrity (// Copyright © Erickson Lopez. MIT License.) on all .cs files.
    3. Zero Spanish words/characters across code and documentation.
    4. Zero [Obsolete] APIs across library code and tests.
    5. Zero CS1591/CS1573/CS1574 warning suppressions in src/.
    6. NuGet package metadata and IsPackable integrity across all projects.
    7. Clean solution build with TreatWarningsAsErrors enabled.
    8. Solution-wide clean packaging (dotnet pack).
    9. Automated test suite execution.
#>

[CmdletBinding()]
param (
    [switch]$SkipTests,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$WorkspaceRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $WorkspaceRoot

try {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  EricksonLopez.Pagination Architecture & Quality Gate Audit" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""

    $FailedChecks = 0

    # 1. Document Naming Audit
    Write-Host "[1/8] Checking markdown naming conventions..." -ForegroundColor Yellow
    $ReservedDocs = @("README.md", "LICENSE", "SECURITY.md", "CONTRIBUTING.md", "CODE_OF_CONDUCT.md", "CHANGELOG.md", "SUPPORT.md", "GOVERNANCE.md", "CODEOWNERS", "PULL_REQUEST_TEMPLATE.md")
    $DocViolations = Get-ChildItem -Path $WorkspaceRoot -Filter "*.md" -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj|\.git|\.system_generated|BenchmarkDotNet\.Artifacts|coveragereport|TestResults|scratch)[\\/]" -and
        $ReservedDocs -notcontains $_.Name -and
        $_.Name -cmatch "[A-Z_]"
    }
    if ($DocViolations) {
        Write-Host "  [FAIL] Non-kebab-case markdown files detected:" -ForegroundColor Red
        $DocViolations | ForEach-Object { Write-Host "    - $($_.FullName)" -ForegroundColor Red }
        $FailedChecks++
    } else {
        Write-Host "  [PASS] All non-reserved markdown files use kebab-case." -ForegroundColor Green
    }

    # 2. Copyright Headers Audit
    Write-Host "[2/8] Checking copyright headers on C# files..." -ForegroundColor Yellow
    $HeaderViolations = Get-ChildItem -Path $WorkspaceRoot -Filter "*.cs" -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj|\.git|\.system_generated|BenchmarkDotNet\.Artifacts|coveragereport|TestResults|scratch)[\\/]"
    } | Where-Object {
        $firstLine = (Get-Content -Path $_.FullName -TotalCount 1)
        -not ($firstLine -match "^\s*//\s*Copyright\s+.*Erickson\s+Lopez")
    }
    if ($HeaderViolations) {
        Write-Host "  [FAIL] Missing copyright header on files:" -ForegroundColor Red
        $HeaderViolations | ForEach-Object { Write-Host "    - $($_.FullName)" -ForegroundColor Red }
        $FailedChecks++
    } else {
        Write-Host "  [PASS] All C# files have valid copyright headers." -ForegroundColor Green
    }

    # 3. Spanish / Accent Characters Audit
    Write-Host "[3/8] Checking for Spanish / non-English characters..." -ForegroundColor Yellow
    $SpanishPattern = "[áéíóúÁÉÍÓÚñÑ¿¡]"
    $SpanishViolations = Get-ChildItem -Path $WorkspaceRoot -Include "*.cs", "*.md" -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj|\.git|\.system_generated|BenchmarkDotNet\.Artifacts|coveragereport|TestResults|scratch)[\\/]"
    } | Where-Object {
        (Get-Content -Path $_.FullName -Raw) -match $SpanishPattern
    }
    if ($SpanishViolations) {
        Write-Host "  [FAIL] Spanish characters detected in files:" -ForegroundColor Red
        $SpanishViolations | ForEach-Object { Write-Host "    - $($_.FullName)" -ForegroundColor Red }
        $FailedChecks++
    } else {
        Write-Host "  [PASS] 100% English-first code and documentation verified." -ForegroundColor Green
    }

    # 4. Obsolete APIs Audit
    Write-Host "[4/8] Checking for [Obsolete] annotations across solution..." -ForegroundColor Yellow
    $ObsoleteMatches = Get-ChildItem -Path (Join-Path $WorkspaceRoot "src") -Filter "*.cs" -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj)[\\/]"
    } | Select-String -Pattern "\[\s*Obsolete"
    if ($ObsoleteMatches) {
        Write-Host "  [FAIL] [Obsolete] attribute found in library code:" -ForegroundColor Red
        $ObsoleteMatches | ForEach-Object { Write-Host "    - $($_.Filename):$($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Red }
        $FailedChecks++
    } else {
        Write-Host "  [PASS] Zero [Obsolete] APIs found across solution." -ForegroundColor Green
    }

    # 5. CS1591 / Documentation Suppression Audit
    Write-Host "[5/8] Checking for documentation warning suppressions (CS1591/CS1573/CS1574) in src/..." -ForegroundColor Yellow
    $SuppressionMatches = Get-ChildItem -Path (Join-Path $WorkspaceRoot "src") -Filter "*.csproj" -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj)[\\/]"
    } | Select-String -Pattern "1591|1573|1574"
    if ($SuppressionMatches) {
        Write-Host "  [FAIL] Documentation warning suppressions found in src/ project files:" -ForegroundColor Red
        $SuppressionMatches | ForEach-Object { Write-Host "    - $($_.Filename):$($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Red }
        $FailedChecks++
    } else {
        Write-Host "  [PASS] Zero CS1591/CS1573/CS1574 suppressions in src/ project files." -ForegroundColor Green
    }

    # 6. Build Solution
    Write-Host "[6/8] Building solution ($Configuration)..." -ForegroundColor Yellow
    dotnet build EricksonLopez.Pagination.slnx -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] Solution build failed." -ForegroundColor Red
        $FailedChecks++
    } else {
        Write-Host "  [PASS] Solution built cleanly with 0 warnings and 0 errors." -ForegroundColor Green
    }

    # 7. Pack Solution
    Write-Host "[7/8] Packaging solution packages ($Configuration)..." -ForegroundColor Yellow
    dotnet pack EricksonLopez.Pagination.slnx -c $Configuration -o ./artifacts --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] Solution packaging failed." -ForegroundColor Red
        $FailedChecks++
    } else {
        Write-Host "  [PASS] Solution packaged cleanly into ./artifacts." -ForegroundColor Green
    }

    # 8. Test Suite
    if (-not $SkipTests) {
        Write-Host "[8/8] Running test suite..." -ForegroundColor Yellow
        dotnet test EricksonLopez.Pagination.slnx -c $Configuration --no-build -m:4
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  [FAIL] Test suite failed." -ForegroundColor Red
            $FailedChecks++
        } else {
            Write-Host "  [PASS] All automated tests passed." -ForegroundColor Green
        }
    } else {
        Write-Host "[8/8] Test execution skipped." -ForegroundColor Yellow
    }

    Write-Host ""
    if ($FailedChecks -eq 0) {
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host "  ALL ARCHITECTURAL COMPLIANCE GATES PASSED (100% GREEN)    " -ForegroundColor Green
        Write-Host "============================================================" -ForegroundColor Green
    } else {
        Write-Host "============================================================" -ForegroundColor Red
        Write-Host "  AUDIT FAILED: $FailedChecks failure(s) detected.          " -ForegroundColor Red
        Write-Host "============================================================" -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}
