<#
run-tests.ps1

Non-interactive test runner: builds the project and runs Unity EditMode tests in batch mode.
This script is intended to be run from the repository root using PowerShell:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-tests.ps1

Exit codes:
  0 = success (Unity exited 0)
  1 = build failure or script error
  2 = Unity executable not found
#>

$ErrorActionPreference = 'Stop'
Write-Host "Running non-interactive test script..."
try {
    Write-Host "Step 1: dotnet build Assembly-CSharp.csproj"
    dotnet build 'Assembly-CSharp.csproj' /property:GenerateFullPaths=true
} catch {
    Write-Host "dotnet build failed: $($_.Exception.Message)"
    exit 1
}

$unityPath = 'C:\Program Files\Unity\Hub\Editor\2021.3.18f1\Editor\Unity.exe'
if (-not (Test-Path $unityPath)) {
    Write-Host "Unity not found at: $unityPath"
    Write-Host "You can edit scripts/run-tests.ps1 to point to your Unity installation or install Unity at the expected path."
    exit 2
}

Write-Host "Step 2: running Unity in batch mode (EditMode tests). This may take several minutes."
New-Item -ItemType Directory -Path (Join-Path $PWD.Path 'TestResults') -Force | Out-Null

$testResultsPath = Join-Path $PWD.Path 'TestResults\editmode-results.xml'
$unityLog = Join-Path $PWD.Path 'TestResults\unity-batch-log.txt'

$args = @('-batchmode', '-projectPath', $PWD.Path, '-runTests', '-testPlatform', 'EditMode', '-testResults', $testResultsPath, '-logFile', $unityLog, '-quit')

Write-Host "Invoking: $unityPath $($args -join ' ')"
try {
    & $unityPath @args
    $unityExit = $LASTEXITCODE
} catch {
    Write-Host "Failed to start Unity: $($_.Exception.Message)"
    exit 1
}

Write-Host "Unity exit code: $unityExit"
if (Test-Path $testResultsPath) { Write-Host "Test results written to: $testResultsPath" } else { Write-Host "No test results file produced; check Unity log: $unityLog" }

exit $unityExit
