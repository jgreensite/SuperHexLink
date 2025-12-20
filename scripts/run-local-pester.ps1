# scripts/run-local-pester.ps1
# Runs Pester unit tests locally with the same settings as CI
Write-Host "Running Pester Unit Tests..." -ForegroundColor Cyan

$res = Invoke-Pester -Script "$PSScriptRoot/tests/Unit" -OutputFormat NUnitXml -OutputFile "pester-results.xml" -PassThru

if ($res.FailedCount -gt 0) { Write-Error "Tests failed!" } else { Write-Host "All tests passed." -ForegroundColor Green }
