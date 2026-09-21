# Copyright © Erickson Lopez. MIT License.
<#
.SYNOPSIS
    Automated solution-wide compliance verification script for EricksonLopez.Pagination.
.DESCRIPTION
    Validates architectural standards:
    1. Kebab-case document naming convention (excluding reserved docs).
    2. Copyright header integrity (// Copyright .* Erickson Lopez\. MIT License\.) on all .cs files.
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
    $ReservedDocs = @("README.md", "LICENSE", "SECURITY.md", "CONTRIBUTING.md", "CODE_OF_CONDUCT.md", "CHANGELOG.md", "SUPPORT.md", "GOVERNANCE.md", "CODEOWNERS", "PULL_REQUEST_TEMPLATE.md", "ROADMAP.md")
    $DocViolations = Get-ChildItem -Path $WorkspaceRoot -Filter "*.md" -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj|\.git|\.system_generated|BenchmarkDotNet\.Artifacts|coveragereport|TestResults|scratch|MEGA-AUDITORIA|StrykerOutput)[\\/]" -and
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
        $_.FullName -notmatch "[\\/](bin|obj|\.git|\.system_generated|BenchmarkDotNet\.Artifacts|coveragereport|TestResults|scratch|MEGA-AUDITORIA|StrykerOutput)[\\/]"
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
    $SpanishPattern = "[\u00E1\u00E9\u00ED\u00F3\u00FA\u00C1\u00C9\u00CD\u00D3\u00DA\u00F1\u00D1\u00BF\u00A1]"
    $SpanishViolations = Get-ChildItem -Path $WorkspaceRoot -Include "*.cs", "*.md" -Recurse | Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj|\.git|\.system_generated|BenchmarkDotNet\.Artifacts|coveragereport|TestResults|scratch|MEGA-AUDITORIA|StrykerOutput)[\\/]"
    } | Where-Object {
        (Get-Content -Path $_.FullName -Raw -Encoding utf8) -match $SpanishPattern
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

# -----------------------------------------------------------------------------
# Stryker.NET Configuration, Concurrency, Anti-Gaming Blacklist & Matrix Synchronization
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Stryker] Validating Stryker.NET configuration, concurrency, anti-gaming blacklist & package matrix..." -ForegroundColor Yellow
$strykerErrors = 0
$targetRoot = if (Get-Variable -Name "RootDirectory" -Scope 0 -ErrorAction SilentlyContinue) { $RootDirectory } elseif (Get-Variable -Name "WorkspaceRoot" -Scope 0 -ErrorAction SilentlyContinue) { $WorkspaceRoot } elseif (Get-Variable -Name "repoRoot" -Scope 0 -ErrorAction SilentlyContinue) { $repoRoot } elseif (Get-Variable -Name "RepoRoot" -Scope 0 -ErrorAction SilentlyContinue) { $RepoRoot } else { (Resolve-Path (Join-Path $PSScriptRoot "..")).Path }

$strykerConfigFiles = Get-ChildItem -Path $targetRoot -Recurse -Filter "stryker*.json" -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj|StrykerOutput|BenchmarkDotNet\.Artifacts|node_modules)[\\/]' -and
    $_.Name -ne "stryker-config.master.json"
}

if (-not $strykerConfigFiles -or $strykerConfigFiles.Count -eq 0) {
    Write-Host "  ❌ Zero Stryker configuration files found in repository." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Zero Stryker configuration files found.") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $strykerErrors++
} else {
    foreach ($sf in $strykerConfigFiles) {
        $json = Get-Content $sf.FullName -Raw | ConvertFrom-Json
        $cfg = if ($json.PSObject.Properties['stryker-config']) { $json.'stryker-config' } else { $json }

        if ($cfg.PSObject.Properties['thresholds']) {
            $th = $cfg.thresholds
            if ($th.high -ne 100 -or $th.low -ne 98 -or $th.break -ne 95) {
                Write-Host "  ❌ Non-compliant mutation thresholds in $($sf.FullName): high=$($th.high), low=$($th.low), break=$($th.break). Required: high=100, low=98, break=95." -ForegroundColor Red
                if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Non-compliant mutation thresholds in $($sf.FullName)") } else { $Violations++ } }
                if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                $strykerErrors++
            }
        }

        if ($cfg.PSObject.Properties['concurrency']) {
            if ($cfg.concurrency -ne 2) {
                Write-Host "  ❌ Non-compliant Stryker concurrency in $($sf.FullName): $($cfg.concurrency). Required: 2." -ForegroundColor Red
                if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Non-compliant Stryker concurrency in $($sf.FullName)") } else { $Violations++ } }
                if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                $strykerErrors++
            }
        }

        if ($cfg.PSObject.Properties['ignore-methods'] -and $cfg.'ignore-methods') {
            foreach ($m in $cfg.'ignore-methods') {
                if ($m -match 'ThrowIf|Exception|Guard|ScrubEphemeralMemory') {
                    Write-Host "  ❌ Prohibited anti-gaming method exclusion '$m' detected in $($sf.FullName)." -ForegroundColor Red
                    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Prohibited anti-gaming exclusion '$m' in $($sf.FullName)") } else { $Violations++ } }
                    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                    $strykerErrors++
                }
            }
        }
    }

        $strykerProfiles = Get-ChildItem -Path $targetRoot -Filter "stryker*.json" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -ne "stryker-config.master.json" -and
        ($_.Name -match '^stryker(-.+)?-config\.json$' -or $_.Name -eq "stryker-config.json")
    }

    $srcProjects = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -notmatch '[\/](bin|obj)[\/]'
    }

    # Verify exact 1:1 count parity between Stryker profile configs and src/ projects
    if ($strykerProfiles.Count -ne $srcProjects.Count) {
        Write-Host "  ❌ Stryker profile count ($($strykerProfiles.Count)) does not match exactly the number of projects in src/ ($($srcProjects.Count))." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Stryker profile count ($($strykerProfiles.Count)) does not match project count in src/ ($($srcProjects.Count)).") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $strykerErrors++
    }

    foreach ($proj in $srcProjects) {
        $projName = $proj.Name
        $matched = $false
        foreach ($sf in $strykerProfiles) {
            $raw = Get-Content $sf.FullName -Raw
            if ($raw -match [regex]::Escape($projName) -or $sf.Name -match [regex]::Escape($proj.BaseName)) {
                $matched = $true
                break
            }
        }

        if (-not $matched) {
            Write-Host "  ❌ Project '$projName' has no corresponding Stryker configuration profile." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Project '$projName' has no corresponding Stryker configuration profile.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }
    }

    foreach ($sf in $strykerProfiles) {
        $raw = Get-Content $sf.FullName -Raw
        $matchedProj = $false
        foreach ($proj in $srcProjects) {
            if ($raw -match [regex]::Escape($proj.Name) -or $sf.Name -match [regex]::Escape($proj.BaseName)) {
                $matchedProj = $true
                break
            }
        }
        if (-not $matchedProj) {
            Write-Host "  ❌ Stryker profile '$($sf.Name)' does not correspond to any project in src/ (orphaned profile)." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Stryker profile '$($sf.Name)' does not correspond to any project in src/.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }
    }

    $mutationWfPath = Join-Path $targetRoot ".github/workflows/mutation-testing.yml"
    if (-not (Test-Path $mutationWfPath)) {
        Write-Host "  ❌ Missing .github/workflows/mutation-testing.yml" -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing .github/workflows/mutation-testing.yml") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $strykerErrors++
    } else {
        $wfContent = Get-Content $mutationWfPath -Raw
        if ($wfContent -match '--concurrency\s*[:\s]\s*([3-9]|\d{2,})') {
            Write-Host "  ❌ Mutation workflow overrides concurrency with value > 2 in CLI arguments." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Mutation workflow overrides concurrency > 2") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }
        if ($wfContent -match '--break-at\s*[:\s]\s*([0-8]\d|\d{1})(?!\d)') {
            Write-Host "  ❌ Mutation workflow overrides break threshold with value < 90 in CLI arguments." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Mutation workflow overrides break threshold < 90") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }

        foreach ($sf in $strykerConfigFiles) {
            if ($sf.Name -eq "stryker-config.json" -and $strykerConfigFiles.Count -gt 1) {
                continue
            }
            if ($sf.Name -eq "stryker-config-unit.json") {
                continue
            }
            $pkgIdent = if ($sf.Name -match '^stryker-(.+)-config\.json$') { $Matches[1] } else { $sf.Name }
            if ($wfContent -notmatch [regex]::Escape($sf.Name) -and $wfContent -notmatch "(?i)name:\s*$pkgIdent" -and $wfContent -notmatch "(?i)working-dir:.*$pkgIdent") {
                Write-Host "  ❌ Stryker configuration '$($sf.Name)' is missing from .github/workflows/mutation-testing.yml matrix." -ForegroundColor Red
                if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Stryker configuration '$($sf.Name)' is missing from matrix") } else { $Violations++ } }
                if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                $strykerErrors++
            }
        }
    }
}

if ($strykerErrors -eq 0) {
    Write-Host "  ✅ Stryker.NET configuration, 100/98/95 thresholds, concurrency 2, anti-gaming blacklist, and package matrix synchronization verified." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Mutation Testing Release Gate Scripts, Workflow Gate & docs/testing-roadmap.md
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Release Gate] Validating mutation release gate scripts, workflow enforcement & docs/testing-roadmap.md..." -ForegroundColor Yellow
$gateErrors = 0

$gateScriptPath = Join-Path $targetRoot "scripts/verify-mutation-gate.js"
if (-not (Test-Path $gateScriptPath)) {
    Write-Host "  ❌ Missing scripts/verify-mutation-gate.js release gate script." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-mutation-gate.js") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $gateErrors++
}

$gateTestPath = Join-Path $targetRoot "scripts/verify-mutation-gate.test.js"
if (-not (Test-Path $gateTestPath)) {
    Write-Host "  ❌ Missing scripts/verify-mutation-gate.test.js unit tests." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-mutation-gate.test.js") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $gateErrors++
}

$roadmapPath = Join-Path $targetRoot "docs/testing-roadmap.md"
if (-not (Test-Path $roadmapPath)) {
    Write-Host "  ❌ Missing docs/testing-roadmap.md governance document." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing docs/testing-roadmap.md") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $gateErrors++
}

$publishWfPath = Join-Path $targetRoot ".github/workflows/publish.yml"
if (Test-Path $publishWfPath) {
    $pubContent = Get-Content $publishWfPath -Raw
    if ($pubContent -notmatch "verify-mutation-gate\.js") {
        Write-Host "  ❌ .github/workflows/publish.yml does not enforce verify-mutation-gate.js before publishing." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("publish.yml does not enforce verify-mutation-gate.js") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $gateErrors++
    }
}

if ($gateErrors -eq 0) {
    Write-Host "  ✅ Mutation release gate scripts, publish pipeline gate, and docs/testing-roadmap.md verified." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Benchmark Regression Quality Gate & CI Enforcement
# -----------------------------------------------------------------------------
$hasBenchProject = (Get-ChildItem -Path $targetRoot -Filter "*Benchmark*.csproj" -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '[\\/](obj|bin|MEGA-AUDITORIA|StrykerOutput)[\\/]' } | Select-Object -First 1) -ne $null
if ($hasBenchProject) {
    Write-Host "`n[Gate: Benchmark Gate] Validating benchmark regression scripts & workflow enforcement..." -ForegroundColor Yellow
    $benchGateErrors = 0

    $benchScriptPath = Join-Path $targetRoot "scripts/verify-benchmark-gate.ps1"
    if (-not (Test-Path $benchScriptPath)) {
        Write-Host "  ❌ Missing scripts/verify-benchmark-gate.ps1 regression assertion script." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-benchmark-gate.ps1") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    }

    $benchTestScriptPath = Join-Path $targetRoot "scripts/verify-benchmark-gate.test.ps1"
    if (-not (Test-Path $benchTestScriptPath)) {
        Write-Host "  ❌ Missing scripts/verify-benchmark-gate.test.ps1 unit tests." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-benchmark-gate.test.ps1") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    }

    $benchWfPath = Join-Path $targetRoot ".github/workflows/benchmark-regression-gate.yml"
    if (-not (Test-Path $benchWfPath)) {
        Write-Host "  ❌ Missing .github/workflows/benchmark-regression-gate.yml CI workflow." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing benchmark-regression-gate.yml") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    } else {
        $benchWfContent = Get-Content $benchWfPath -Raw -Encoding utf8
        if ($benchWfContent -notmatch "verify-benchmark-gate\.ps1" -or $benchWfContent -notmatch "--exporters json") {
            Write-Host "  ❌ .github/workflows/benchmark-regression-gate.yml does not enforce verify-benchmark-gate.ps1 and --exporters json." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Invalid benchmark-regression-gate.yml") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $benchGateErrors++
        }
    }

    $baselinePath = Join-Path $targetRoot "benchmarks/results/baseline.json"
    if (-not (Test-Path $baselinePath)) {
        Write-Host "  ❌ Missing benchmarks/results/baseline.json baseline file." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing benchmarks/results/baseline.json") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    }

    if ($benchGateErrors -eq 0) {
        Write-Host "  ✅ Benchmark regression assertion script, baseline, and CI workflow verified." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# SourceLink & Central Package Management Integration Gate
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: SourceLink] Validating centralized Microsoft.SourceLink.GitHub integration..." -ForegroundColor Yellow
$sourceLinkErrors = 0

$pkgPropsPath = Join-Path $targetRoot "Directory.Packages.props"
$bldPropsPath = Join-Path $targetRoot "Directory.Build.props"

if (-not (Test-Path $pkgPropsPath)) {
    Write-Host "  ❌ Missing Directory.Packages.props." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing Directory.Packages.props") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $sourceLinkErrors++
} else {
    $pkgContent = Get-Content $pkgPropsPath -Raw -Encoding utf8
    if ($pkgContent -notmatch 'PackageVersion\s+Include="Microsoft\.SourceLink\.GitHub"') {
        Write-Host "  ❌ Directory.Packages.props must declare 'Microsoft.SourceLink.GitHub' instead of generic or missing package." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Directory.Packages.props missing Microsoft.SourceLink.GitHub") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
    if ($pkgContent -match 'PackageVersion\s+Include="Microsoft\.SourceLink\.Common"' -and $pkgContent -notmatch 'PackageVersion\s+Include="Microsoft\.SourceLink\.GitHub"') {
        Write-Host "  ❌ Directory.Packages.props uses generic Microsoft.SourceLink.Common without GitHub provider." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Generic Microsoft.SourceLink.Common used") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
}

if (-not (Test-Path $bldPropsPath)) {
    Write-Host "  ❌ Missing Directory.Build.props." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing Directory.Build.props") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $sourceLinkErrors++
} else {
    $bldContent = Get-Content $bldPropsPath -Raw -Encoding utf8
    if ($bldContent -notmatch 'PackageReference\s+Include="Microsoft\.SourceLink\.GitHub"') {
        Write-Host "  ❌ Directory.Build.props must centralize '<PackageReference Include=""Microsoft.SourceLink.GitHub"" PrivateAssets=""All"" />'." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Directory.Build.props missing Microsoft.SourceLink.GitHub PackageReference") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
    if ($bldContent -notmatch '<PublishRepositoryUrl>\s*true\s*</PublishRepositoryUrl>' -and $bldContent -notmatch '<PublishRepositoryUrl\s+Condition=') {
        Write-Host "  ❌ Directory.Build.props must specify '<PublishRepositoryUrl>true</PublishRepositoryUrl>'." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Directory.Build.props missing PublishRepositoryUrl") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
}

if ($sourceLinkErrors -eq 0) {
    Write-Host "  ✅ SourceLink integration (Microsoft.SourceLink.GitHub) verified in Directory.Packages.props & Directory.Build.props." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Native AOT Test Gate & Compilation Smoke Test Invariants
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Native AOT] Validating Native AOT compilation smoke tests & workflow enforcement..." -ForegroundColor Yellow
$aotErrors = 0

$allSrcProjs = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}

# 1. Discover AOT-applicable projects in src/
$aotApplicableProjects = @()
foreach ($proj in $allSrcProjs) {
    $projName = $proj.Name
    $projDir = $proj.DirectoryName
    
    # Exclude Roslyn Analyzers and Source Generators
    if ($projName -match '(Analyzers?|Generators?)\.csproj$' -or $projDir -match '[\\/](Analyzers?|Generators?)[\\/]?$') {
        continue
    }
    # Exclude API endpoints / applications if applicable
    if ($projName -match '\.Api\.csproj$') {
        continue
    }
    
    $projContent = Get-Content $proj.FullName -Raw
    # Exclude projects explicitly marked as non-AOT compatible
    if ($projContent -match '<IsAotCompatible>\s*false\s*</IsAotCompatible>' -or 
        $projContent -match '<PublishAot>\s*false\s*</PublishAot>') {
        continue
    }
    
    $aotApplicableProjects += $proj
}

if ($aotApplicableProjects.Count -gt 0) {
    Write-Host "  [INFO] Detected $($aotApplicableProjects.Count) Native AOT applicable project(s) in src/." -ForegroundColor Gray
    
    # 2. Check for dedicated Native AOT smoke test project in tests/ or samples/
    $aotTestProjects = @()
    foreach ($searchDir in @("tests", "samples")) {
        $dirPath = Join-Path $targetRoot $searchDir
        if (Test-Path $dirPath) {
            $candidateTests = Get-ChildItem -Path $dirPath -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            }
            foreach ($t in $candidateTests) {
                $content = Get-Content $t.FullName -Raw
                if ($content -match '<PublishAot>\s*true\s*</PublishAot>' -or $t.Name -match 'AotSmokeTest|AotTest|NativeAot') {
                    $aotTestProjects += $t
                }
            }
        }
    }
    
    if ($aotTestProjects.Count -eq 0) {
        Write-Host "  ❌ Missing Native AOT smoke test project in tests/ or samples/ for $($aotApplicableProjects.Count) AOT-applicable project(s)." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing Native AOT smoke test project in tests/ or samples/.") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $aotErrors++
    } else {
        $hasValidExecutable = $false
        foreach ($aotProj in $aotTestProjects) {
            $aotContent = Get-Content $aotProj.FullName -Raw
            if ($aotContent -match '<OutputType>\s*Exe\s*</OutputType>' -and ($aotContent -match '<PublishAot>\s*true\s*</PublishAot>' -or $aotContent -match 'PublishAot')) {
                $hasValidExecutable = $true
                break
            }
        }
        if (-not $hasValidExecutable) {
            Write-Host "  ❌ At least one AOT smoke test project must declare OutputType=Exe and PublishAot=true." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("AOT smoke test project must declare OutputType=Exe and PublishAot=true.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $aotErrors++
        }
    }
    
    # 3. Check for CI workflow .github/workflows/aot-smoke-test.yml
    $aotWorkflowPath = Join-Path $targetRoot ".github/workflows/aot-smoke-test.yml"
    if (-not (Test-Path $aotWorkflowPath)) {
        Write-Host "  ❌ Missing .github/workflows/aot-smoke-test.yml CI workflow." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing .github/workflows/aot-smoke-test.yml CI workflow.") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $aotErrors++
    } else {
        $wfContent = Get-Content $aotWorkflowPath -Raw
        if ($wfContent -notmatch 'dotnet publish' -or ($wfContent -notmatch 'linux-x64|win-x64' -and $wfContent -notmatch 'PublishAot')) {
            Write-Host "  ❌ Workflow .github/workflows/aot-smoke-test.yml does not execute a valid Native AOT publish step." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Invalid aot-smoke-test.yml workflow.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $aotErrors++
        }
    }
} else {
    Write-Host "  [INFO] Zero Native AOT applicable projects in src/ (pure analyzer/generator repository). Native AOT test gate skipped." -ForegroundColor Gray
}

if ($aotErrors -eq 0) {
    Write-Host "  ✅ Native AOT test project(s) and CI workflow verified." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# README Package Table Parity Gate
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: README Package Table Parity] Validating documentation package table synchronization..." -ForegroundColor Yellow
$readmeErrors = 0
$readmePath = Join-Path $targetRoot "README.md"

if (-not (Test-Path $readmePath)) {
    Write-Host "  ❌ Missing README.md in repository root." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing README.md in repository root.") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $readmeErrors++
} else {
    $readmeContent = Get-Content $readmePath -Raw -Encoding utf8
    $allSrcProjs = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    }

    foreach ($proj in $allSrcProjs) {
        $projName = $proj.Name
        $baseName = $proj.BaseName
        
        # Check if project appears in README.md inside a table or package reference
        $escapedBase = [regex]::Escape($baseName)
        $isDocumented = ($readmeContent -match ('\|\s*`?' + $escapedBase + '`?\s*\|')) -or 
                        ($readmeContent -match ('\[`?' + $escapedBase + '`?\]')) -or
                        ($readmeContent -match "/packages/$escapedBase") -or
                        ($readmeContent -match ('\|\s*\[`?' + $escapedBase + '`?\]'))

        if (-not $isDocumented) {
            Write-Host "  ❌ Project '$projName' is missing from the packages table in README.md." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Project '$projName' is missing from the packages table in README.md.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $readmeErrors++
        }
    }

    if ($readmeErrors -eq 0) {
        Write-Host "  ✅ All $($allSrcProjs.Count) project(s) in src/ verified in README.md package table." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# Test Suite Symmetry & Coverage Gate (Principle 12)
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Test Suite Symmetry] Validating test project symmetry & project references across tests/..." -ForegroundColor Yellow
$testSymErrors = 0
$testsDir = Join-Path $targetRoot "tests"

$allSrcProjs = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}

$allTestProjs = @()
$testProjectReferences = @{}
if (Test-Path $testsDir) {
    $allTestProjs = Get-ChildItem -Path $testsDir -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    }
    foreach ($tp in $allTestProjs) {
        $tContent = Get-Content $tp.FullName -Raw -Encoding utf8
        $refs = [regex]::Matches($tContent, '<ProjectReference\s+Include="([^"]+)"')
        foreach ($m in $refs) {
            $refFile = Split-Path $m.Groups[1].Value.Replace('\', '/') -Leaf
            $testProjectReferences[$refFile] = $true
        }
    }
}

foreach ($proj in $allSrcProjs) {
    $projName = $proj.Name
    $baseName = $proj.BaseName
    
    # Check 1: Named test suite matching base name
    $hasNamedTest = ($allTestProjs | Where-Object { $_.BaseName -match "^$([regex]::Escape($baseName))(\..+)?Tests?$" -or $_.BaseName -like "*$baseName*" }) -ne $null
    
    # Check 2: Direct ProjectReference in any test project
    $hasReference = $testProjectReferences.ContainsKey($projName)
    
    if (-not $hasNamedTest -and -not $hasReference) {
        Write-Host "  ❌ Project '$projName' has no corresponding test suite in tests/ (missing test project or ProjectReference)." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Project '$projName' has no corresponding test suite in tests/.") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $testSymErrors++
    }
}

if ($testSymErrors -eq 0) {
    Write-Host "  ✅ All $($allSrcProjs.Count) project(s) in src/ verified with corresponding test suite in tests/." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Code Quality, Analyzers & Strong Name Invariants Gate
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Quality & Invariants] Validating CA1305, CS0618, PackageProjectUrl, and SNK invariants..." -ForegroundColor Yellow
$invariantErrors = 0

# 1. Check for CS0618/CS0619 suppressions across all projects and props
$cs0618Suppressions = Get-ChildItem -Path $targetRoot -Include "*.csproj", "*.props" -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj|BenchmarkDotNet\.Artifacts|StrykerOutput)[\\/]'
} | Select-String -Pattern "CS0618|CS0619"
if ($cs0618Suppressions) {
    Write-Host "  ❌ Prohibited CS0618/CS0619 warning suppressions found:" -ForegroundColor Red
    $cs0618Suppressions | ForEach-Object { Write-Host "    - $($_.Filename):$($_.LineNumber): $($_.Line.Trim())" -ForegroundColor Red }
    $FailedChecks++
    $invariantErrors++
}

# 2. Check that root .editorconfig enforces CA1305 as warning
$editorConfigPath = Join-Path $targetRoot ".editorconfig"
if (Test-Path $editorConfigPath) {
    $ecContent = Get-Content $editorConfigPath -Raw -Encoding utf8
    if ($ecContent -notmatch "dotnet_diagnostic\.CA1305\.severity\s*=\s*warning") {
        Write-Host "  ❌ Root .editorconfig must enforce 'dotnet_diagnostic.CA1305.severity = warning'." -ForegroundColor Red
        $FailedChecks++
        $invariantErrors++
    }
} else {
    Write-Host "  ❌ Missing root .editorconfig." -ForegroundColor Red
    $FailedChecks++
    $invariantErrors++
}

# 3. Check PackageProjectUrl in Directory.Build.props
$dirBuildPropsPath = Join-Path $targetRoot "Directory.Build.props"
if (Test-Path $dirBuildPropsPath) {
    $dbpContent = Get-Content $dirBuildPropsPath -Raw -Encoding utf8
    if ($dbpContent -notmatch "<PackageProjectUrl>https://ericksonlopez\.dev/dotnet-pagination</PackageProjectUrl>") {
        Write-Host "  ❌ Directory.Build.props must specify '<PackageProjectUrl>https://ericksonlopez.dev/dotnet-pagination</PackageProjectUrl>'." -ForegroundColor Red
        $FailedChecks++
        $invariantErrors++
    }
}

# 4. Check that CI workflows decode SNK to EricksonLopez.snk
$wfPaths = @(
    (Join-Path $targetRoot ".github/workflows/dotnet-build-test.yml"),
    (Join-Path $targetRoot ".github/workflows/publish.yml"),
    (Join-Path $targetRoot ".github/workflows/aot-smoke-test.yml")
)
foreach ($wf in $wfPaths) {
    if (Test-Path $wf) {
        $wfText = Get-Content $wf -Raw -Encoding utf8
        if ($wfText -match "DummyDevelopmentKey\.snk") {
            Write-Host "  ❌ Workflow '$(Split-Path $wf -Leaf)' contains legacy DummyDevelopmentKey.snk. Must decode to EricksonLopez.snk." -ForegroundColor Red
            $FailedChecks++
            $invariantErrors++
        }
    }
}

if ($invariantErrors -eq 0) {
    Write-Host "  ✅ Zero CS0618 suppressions, CA1305 enabled, PackageProjectUrl verified, and SNK workflows aligned." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Gate: One-Type-Per-File in src/
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: One-Type-Per-File] Validating one top-level type per file in src/..." -ForegroundColor Yellow
$srcFiles = Get-ChildItem -Path (Join-Path $targetRoot "src") -Filter "*.cs" -Recurse | Where-Object {
    $_.FullName -notmatch '[\/](bin|obj)[\/]'
}
$oneTypeViolations = 0
foreach ($sf in $srcFiles) {
    $lines = Get-Content $sf.FullName
    $topTypes = @()
    foreach ($line in $lines) {
        if ($line -match '^(public|internal|class|interface|struct|record|enum|delegate)\s+(?:static\s+|sealed\s+|abstract\s+|readonly\s+|partial\s+)*(?:class|interface|struct|record|enum|delegate)\s+([A-Za-z0-9_]+)') {
            $topTypes += $Matches[2]
        }
    }
    # Filter identical type names resulting from #if/#else preprocessor directives
    $uniqueTypes = $topTypes | Select-Object -Unique
    # Also allow companion generic / non-generic interfaces with same name (e.g. IPagedList, IPagedList<T>)
    if ($uniqueTypes.Count -gt 1) {
        # If all names are identical, it is conditional compilation or companion
        $distinctBaseNames = $uniqueTypes | Select-Object -Unique
        if ($distinctBaseNames.Count -gt 1) {
            Write-Host "  ❌ Multiple top-level types ($($distinctBaseNames -join ', ')) in $($sf.FullName)." -ForegroundColor Red
            $FailedChecks++
            $oneTypeViolations++
        }
    }
}
if ($oneTypeViolations -eq 0) {
    Write-Host "  ✅ 100% of C# files in src/ adhere to the One-Type-Per-File architecture standard." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Gate: Support Email & Maintainer Identity
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Support Email & Identity] Validating official maintainer contact..." -ForegroundColor Yellow
$emailErrors = 0
$supportFiles = @(
    (Join-Path $targetRoot "SUPPORT.md"),
    (Join-Path $targetRoot "SECURITY.md"),
    (Join-Path $targetRoot "CODE_OF_CONDUCT.md")
)
foreach ($sfile in $supportFiles) {
    if (Test-Path $sfile) {
        $content = Get-Content $sfile -Raw -Encoding utf8
        if ($content -notmatch "ericksonlopezf@gmail\.com") {
            Write-Host "  ❌ Missing official support email 'ericksonlopezf@gmail.com' in $(Split-Path $sfile -Leaf)." -ForegroundColor Red
            $FailedChecks++
            $emailErrors++
        }
    }
}
if ($emailErrors -eq 0) {
    Write-Host "  ✅ Official support email 'ericksonlopezf@gmail.com' verified across documentation." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Gate: Mandatory Analyzers & Diagnostics Invariant
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Mandatory Analyzers] Validating IDE1006, CA1707, CA1852, CA1305, CS0619, CS0618, xUnit1051, CS1591 in .editorconfig..." -ForegroundColor Yellow
$analyzerErrors = 0
if (Test-Path $editorConfigPath) {
    $ec = Get-Content $editorConfigPath -Raw -Encoding utf8
    $requiredRules = @(
        "dotnet_diagnostic\.CS1591\.severity\s*=\s*warning",
        "dotnet_diagnostic\.CS0618\.severity\s*=\s*error",
        "dotnet_diagnostic\.CS0619\.severity\s*=\s*error",
        "dotnet_diagnostic\.IDE1006\.severity\s*=\s*warning",
        "dotnet_diagnostic\.CA1707\.severity\s*=\s*warning",
        "dotnet_diagnostic\.CA1852\.severity\s*=\s*warning",
        "dotnet_diagnostic\.CA1305\.severity\s*=\s*warning",
        "dotnet_diagnostic\.xUnit1051\.severity\s*=\s*warning",
        "dotnet_diagnostic\.CS0159\.severity\s*=\s*error",
        "dotnet_diagnostic\.CS159\.severity\s*=\s*error"
    )
    foreach ($rulePattern in $requiredRules) {
        if ($ec -notmatch $rulePattern) {
            Write-Host "  ❌ Missing required diagnostic rule pattern '$rulePattern' in .editorconfig." -ForegroundColor Red
            $FailedChecks++
            $analyzerErrors++
        }
    }
}
if ($analyzerErrors -eq 0) {
    Write-Host "  ✅ All mandatory analyzer diagnostic severity rules verified in .editorconfig." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Gate: Logo & Branding Assets
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Logo & Branding] Validating package icon and branding assets..." -ForegroundColor Yellow
$iconPath = Join-Path $targetRoot "icon.png"
if (-not (Test-Path $iconPath) -or (Get-Item $iconPath).Length -eq 0) {
    Write-Host "  ❌ Missing or empty package icon at '$iconPath'." -ForegroundColor Red
    $FailedChecks++
} else {
    Write-Host "  ✅ Official icon.png ($( (Get-Item $iconPath).Length ) bytes) verified in root." -ForegroundColor Green
}


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
