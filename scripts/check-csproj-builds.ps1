<#
Runs `dotnet build` for every .csproj under the repository and returns non-zero if any build fails.
This is a lightweight guard that CI and local pre-push hooks can call to catch compile errors quickly.
#>
Param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Write-Host "Repository root: $root"

$csprojs = Get-ChildItem -Path $root -Recurse -Filter *.csproj -ErrorAction SilentlyContinue | 
    Where-Object { 
        $_.FullName -notmatch '\\Sirenix\\' -and 
        $_.FullName -notmatch 'actions-runner' -and
        $_.Name -notmatch '^Sirenix\.'
    } |
    Select-Object -ExpandProperty FullName

if (-not $csprojs) {
    Write-Host "No .csproj files found under repository. Nothing to build." -ForegroundColor Yellow
    exit 0
}

$failed = @()
foreach ($proj in $csprojs) {
    Write-Host "Building: $proj"
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList @('build', $proj, '/property:GenerateFullPaths=true', '/consoleloggerparameters:NoSummary') -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        Write-Host "Build failed: $proj (exit $($proc.ExitCode))" -ForegroundColor Red
        $failed += $proj
    } else {
        Write-Host "Build succeeded: $proj" -ForegroundColor Green
    }
}

if ($failed.Count -gt 0) {
    Write-Host "One or more projects failed to build:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "All projects built successfully." -ForegroundColor Green
exit 0
