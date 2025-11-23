param([string]$RunId = '18627502922', [int]$Attempts=12)
for ($i=0; $i -lt $Attempts; $i++) {
  $s = gh run view $RunId --repo jgreensite/SuperHexLink --json status --jq .status 2>$null
  Write-Host ("[{0}] attempt {1}/{2} status: {3}" -f (Get-Date -Format o), ($i+1), $Attempts, $s)
  if ($s -eq 'completed') { exit 0 }
  Start-Sleep -Seconds 5
}
Write-Host 'Timed out waiting for run to complete'; exit 2
