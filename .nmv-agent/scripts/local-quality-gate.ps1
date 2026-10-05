param(
    [string]$SettingsPath = ".nmv-agent/project-settings.json"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SettingsPath)) {
    throw "Missing settings file: $SettingsPath"
}

$settings = Get-Content $SettingsPath -Raw | ConvertFrom-Json

function Run-Step([string]$Name, [string]$Command) {
    Write-Host ""
    Write-Host "== $Name ==" -ForegroundColor Cyan
    Write-Host $Command
    & powershell -NoProfile -Command $Command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$Name FAILED" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "$Name PASSED" -ForegroundColor Green
}

if ($settings.build.restore) { Run-Step "Restore" $settings.build.restore }
if ($settings.qualityGate.requireBuild -and $settings.build.build) { Run-Step "Build" $settings.build.build }
if ($settings.qualityGate.requireTests -and $settings.build.test) { Run-Step "Tests" $settings.build.test }

Write-Host ""
Write-Host "LOCAL QUALITY GATE PASSED" -ForegroundColor Green

