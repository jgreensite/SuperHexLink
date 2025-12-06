<#
Runs Unity in batch mode to execute EditMode tests using the AutomatedTestRunner.RunEditModeTests method.
Requires Unity to be installed on the machine. Set UNITY_EXECUTABLE environment variable to the full path
of the Unity editor executable if it's not on PATH.

Usage:
  powershell -File scripts\run-unity-editmode-tests.ps1
#>
Param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent

$unityExe = $env:UNITY_EXECUTABLE
if (-not $unityExe) {
    # Try common Windows install locations for Unity Hub/Editor (user may need to tweak)
    $possible = @(
        "$env:ProgramFiles\Unity\Hub\Editor\2021.3.18f1\Editor\Unity.exe",
        "$env:ProgramFiles\Unity\Editor\Unity.exe",
        "$env:ProgramFiles(x86)\Unity\Editor\Unity.exe"
    )
    foreach ($p in $possible) { if (Test-Path $p) { $unityExe = $p; break } }
}

if (-not $unityExe -or -not (Test-Path $unityExe)) {
    Write-Host "Unity executable not found. Set UNITY_EXECUTABLE env var to the editor executable path." -ForegroundColor Yellow
    exit 2
}

$projectPath = $repoRoot
$logFile = Join-Path $repoRoot 'logs\unity-editmode-tests.log'
if (-not (Test-Path (Split-Path $logFile -Parent))) { New-Item -ItemType Directory -Path (Split-Path $logFile -Parent) | Out-Null }

$args = @(
    '-batchmode',
    '-nographics',
    "-projectPath", "$projectPath",
    '-executeMethod', 'AutomatedTestRunner.RunEditModeTests',
    "-logFile", "$logFile",
    '-quit'
)

Write-Host "Running Unity: $unityExe $($args -join ' ')"
$proc = Start-Process -FilePath $unityExe -ArgumentList $args -NoNewWindow -Wait -PassThru
Write-Host "Unity exit code: $($proc.ExitCode)"
exit $proc.ExitCode
