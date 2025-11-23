# Check self-hosted actions runner processes and diagnostics
$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$dest = Join-Path $scriptRoot 'actions-runner'

Write-Host "Checking processes via Win32_Process (CIM)..."
try {
    $procs = Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
        ($_.CommandLine -and ($_.CommandLine -match 'Runner.Listener' -or $_.CommandLine -match 'run.cmd' -or $_.CommandLine -match 'actions-runner')) -or
        ($_.ExecutablePath -and ($_.ExecutablePath -like '*actions-runner*' -or $_.ExecutablePath -match 'Runner.Listener'))
    }
} catch {
    Write-Host "Get-CimInstance failed: $_" -ForegroundColor Yellow
    $procs = @()
}

if ($procs -and $procs.Count -gt 0) {
    Write-Host "Found runner-related processes:"
    $procs | Select-Object ProcessId, CommandLine, ExecutablePath | ForEach-Object {
        Write-Host "PID:$($_.ProcessId)"
        if ($_.ExecutablePath) { Write-Host "  Path: $($_.ExecutablePath)" }
        if ($_.CommandLine) { Write-Host "  Cmd: $($_.CommandLine)" }
    }
} else {
    Write-Host "No runner-related processes found via CIM. Listing top processes that may be the runner (dotnet, node):"
    Get-Process dotnet,node -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, Path | ForEach-Object { Write-Host "PID:$($_.Id) Name:$($_.ProcessName) Path:$($_.Path)" }
}

# Tail latest diag log
$diag = Join-Path $dest '_diag'
if (Test-Path $diag) {
    $file = Get-ChildItem -LiteralPath $diag -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($file) {
        Write-Host "\nLatest diag file: $($file.Name) (LastWrite: $($file.LastWriteTime))"
        Write-Host "--- last 200 lines ---"
        Get-Content -LiteralPath $file.FullName -Tail 200 | ForEach-Object { Write-Host $_ }
    } else { Write-Host "No diag files found in $diag" }
} else { Write-Host "No _diag folder at $diag" }
