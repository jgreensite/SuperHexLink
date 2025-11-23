# Run from repo root with: powershell -File scripts\check-runner-state.ps1
Set-Location -Path $PSScriptRoot\..
Write-Host '== GitHub repository runners =='
try {
  gh api /repos/jgreensite/SuperHexLink/actions/runners --jq ".runners[] | {id: .id, name: .name, status: .status, labels: [.labels[].name]}" 2>$null
} catch {
  Write-Host 'Failed to query GH API - ensure gh CLI is logged in.'
}

Write-Host "`n== Local runner processes =="
Get-CimInstance Win32_Process -Filter "Name='Runner.Listener.exe' OR Name='cmd.exe'" | Where-Object { $_.CommandLine -and ($_.CommandLine -match 'actions-runner' -or $_.CommandLine -match 'run.cmd') } | Select-Object ProcessId, Name, CommandLine | Format-List

Write-Host "`n== Latest runner diagnostic tail =="
$diagDir = Join-Path $PSScriptRoot 'actions-runner\_diag'
if (Test-Path $diagDir) {
  $f = Get-ChildItem -Path $diagDir -Filter 'Runner_*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
  if ($f) {
    Write-Host ('Tailing last 200 lines of {0}' -f $f.FullName)
    Get-Content -LiteralPath $f.FullName -Tail 200
  } else {
    Write-Host 'No runner diag log file found in' $diagDir
  }
} else {
  Write-Host 'Diag directory not found:' $diagDir
}
