<#
Lightweight CLI wrapper around check-csproj-builds.ps1.
This script provides a well-defined command-line surface that can be invoked by CI and harnesses.
It forwards supported parameters to the underlying wrapper and supports -DryRun and -WhatIf shaping.
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param(
    [switch]$ChangedOnly,
    [string]$DiffRef,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
$wrapper = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'
if (-not (Test-Path $wrapper)) { Write-Host "Wrapper not found at $wrapper" -ForegroundColor Red; exit 2 }

# Evaluate ShouldProcess semantics here as the top-level CLI
if ($PSCmdlet -and $PSCmdlet.ShouldProcess('Build guard', 'Evaluate/Run')) {
    $args = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$wrapper)
    if ($ChangedOnly) { $args += '-ChangedOnly' }
    if ($DiffRef) { $args += @('-DiffRef',$DiffRef) }
    if ($DryRun -or $PSBoundParameters.ContainsKey('WhatIf')) { $args += '-DryRun' }

    # Prefer pwsh if available, fallback to powershell.exe
    if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
    $proc = Start-Process -FilePath $exe -ArgumentList $args -NoNewWindow -Wait -PassThru
    exit $proc.ExitCode
}

Write-Host 'Aborted by ShouldProcess/WhatIf.' -ForegroundColor Yellow
exit 0
