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

function Invoke-CheckCsprojBuildsCli {
    param(
        [switch]$ChangedOnly,
        [string]$DiffRef,
        [switch]$DryRun
    )

    # Derive the script location robustly so function works when dot-sourced in tests
    $scriptPath = if ($PSCommandPath) { $PSCommandPath } elseif ($MyInvocation.MyCommand.Definition) { $MyInvocation.MyCommand.Definition } else { $null }
    $repoRoot = if ($scriptPath) { Split-Path -Path $scriptPath -Parent | Split-Path -Parent } else { Write-Verbose 'Unable to determine script path'; $null }
    $wrapper = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'
    if (-not (Test-Path $wrapper)) { Write-Host "Wrapper not found at $wrapper" -ForegroundColor Red; return 2 }

    if ($PSCmdlet -and $PSCmdlet.ShouldProcess('Build guard', 'Evaluate/Run')) {
        $args = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$wrapper)
        if ($ChangedOnly) { $args += '-ChangedOnly' }
        if ($DiffRef) { $args += @('-DiffRef',$DiffRef) }
        if ($DryRun -or $PSBoundParameters.ContainsKey('WhatIf')) { $args += '-DryRun' }

        if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
        $proc = Start-Process -FilePath $exe -ArgumentList $args -NoNewWindow -Wait -PassThru
        return $proc.ExitCode
    }

    Write-Host 'Aborted by ShouldProcess/WhatIf.' -ForegroundColor Yellow
    return 0
}

# If script invoked directly, call the function with parameters
if ($MyInvocation.InvocationName -ne '.') { exit (Invoke-CheckCsprojBuildsCli -ChangedOnly:$ChangedOnly -DiffRef $DiffRef -DryRun:$DryRun) }
