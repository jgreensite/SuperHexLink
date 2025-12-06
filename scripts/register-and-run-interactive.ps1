<#
register-and-run-interactive.ps1

Stops any existing runner processes, requests a repository registration token,
configures the runner with that token (unattended), and starts run.cmd
interactively so operator can see live logs.

Run from repo root:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\register-and-run-interactive.ps1
#>

$ErrorActionPreference = 'Stop'
Write-Host "Stopping existing runner processes..."
Get-CimInstance Win32_Process -Filter "Name='Runner.Listener.exe' OR Name='cmd.exe'" | ForEach-Object {
    try { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue; Write-Host "Stopped PID:$($_.ProcessId)" } catch { }
}

$runnerDir = Join-Path $PSScriptRoot 'actions-runner'
if (-not (Test-Path $runnerDir)) { Write-Host "Runner dir missing: $runnerDir"; exit 2 }

Set-Location -Path $runnerDir

Write-Host "Requesting repository registration token..."
$token = gh api -X POST /repos/jgreensite/SuperHexLink/actions/runners/registration-token --jq .token
if (-not $token) { Write-Host 'Failed to obtain registration token'; exit 3 }
Write-Host "Configuring runner (unattended --replace)"
& .\config.cmd --unattended --replace --url 'https://github.com/jgreensite/SuperHexLink' --token $token --name 'selfhost-GAMINGPC' --labels 'self-hosted,windows,unity'

Write-Host 'Starting run.cmd interactively (will stream logs). Press Ctrl+C to stop.'
& .\run.cmd
