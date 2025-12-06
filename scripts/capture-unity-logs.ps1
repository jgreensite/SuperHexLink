# Capture likely Unity Editor/Player logs into repo and print tails
Set-Location -Path $PSScriptRoot\..\
$outFile = Join-Path 'scripts' 'unity-editor-log-tail.txt'
Remove-Item -LiteralPath $outFile -ErrorAction SilentlyContinue

$tests = @()
$tests += Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log'
$tests += Join-Path $env:USERPROFILE 'AppData\LocalLow\Unity\Editor\Editor.log'
$tests += Join-Path $env:USERPROFILE 'AppData\LocalLow\Unity\Player.log'
$tests += Join-Path $env:USERPROFILE 'AppData\Local\Temp\Unity\Editor.log'

# Add any Editor.log files under AppData quickly (limit depth by filtering common subfolders)
$searchRoots = @(
    Join-Path $env:USERPROFILE 'AppData\LocalLow',
    Join-Path $env:USERPROFILE 'AppData\Local',
    Join-Path $env:LOCALAPPDATA ''
)

foreach ($root in $searchRoots) {
    if (Test-Path $root) {
        try {
            $found = Get-ChildItem -Path $root -Filter 'Editor.log' -File -Recurse -ErrorAction SilentlyContinue -Force | Select-Object -First 5
            foreach ($f in $found) { $tests += $f.FullName }
        } catch { }
        try {
            $found2 = Get-ChildItem -Path $root -Filter 'Player.log' -File -Recurse -ErrorAction SilentlyContinue -Force | Select-Object -First 5
            foreach ($f in $found2) { $tests += $f.FullName }
        } catch { }
    }
}

$unique = $tests | Where-Object { $_ } | Select-Object -Unique
if (-not $unique) {
    Write-Host 'No Unity Editor or Player logs found in common locations.'
    Write-Host 'Please open Unity Editor with the project now (File -> Open Project) and then re-run this script.'
    exit 2
}

Add-Content -Path $outFile -Value "=== Unity logs captured on $(Get-Date -Format o) ===`n"
foreach ($p in $unique) {
    Add-Content -Path $outFile -Value "\n--- $p ---\n"
    try {
        Get-Content -LiteralPath $p -Tail 800 -ErrorAction Stop | Out-File -Append -FilePath $outFile -Encoding utf8
    } catch {
        Add-Content -Path $outFile -Value "(failed to read $p)"
    }
}

Write-Host "Saved captured logs to $outFile"
Write-Host '--- Tail (last 200 lines) ---'
Get-Content -LiteralPath $outFile -Tail 200 | Write-Host
